using System.Security.Claims;
using ServicioEmpleados.Contracts.Common;
using ServicioEmpleados.Contracts.Employees;
using ServicioEmpleados.Application.Employees.Models;
using ServicioEmpleados.Application.Employees.Ports.Input;
using ServicioEmpleados.Domain.Audit.Entities;
using ServicioEmpleados.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ServicioEmpleados.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(IEmployeeManagementUseCase employeeManagementUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<EmployeePageResponse>>> GetPageAsync(
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "apellidos",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] long anchorEmployeeId = 0,
        [FromQuery] long cursorEmployeeId = 0,
        [FromQuery] bool isNextPage = true,
        [FromQuery] string searchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Clamp(pageSize, 1, 50);
        var employeesResult = cursorEmployeeId > 0
            ? await employeeManagementUseCase.GetPageByCursorAsync(
                effectivePageSize,
                sortBy,
                sortDirection,
                cursorEmployeeId,
                isNextPage,
                searchTerm,
                cancellationToken)
            : await employeeManagementUseCase.GetPageFromAnchorAsync(
                effectivePageSize,
                sortBy,
                sortDirection,
                anchorEmployeeId,
                searchTerm,
                cancellationToken);
        if (employeesResult.IsFailure)
        {
            return ToFailureAction<EmployeePageResponse>(employeesResult);
        }

        var employees = employeesResult.Value;

        var hasPreviousPage = false;
        var hasNextPage = false;
        if (employees.Count > 0)
        {
            var hasPreviousResult = await employeeManagementUseCase.HasEmployeesByCursorAsync(
                sortBy,
                sortDirection,
                employees[0].Id,
                isNextPage: false,
                searchTerm,
                cancellationToken);
            if (hasPreviousResult.IsFailure)
            {
                return ToFailureAction<EmployeePageResponse>(hasPreviousResult);
            }
            hasPreviousPage = hasPreviousResult.Value;

            var hasNextResult = await employeeManagementUseCase.HasEmployeesByCursorAsync(
                sortBy,
                sortDirection,
                employees[^1].Id,
                isNextPage: true,
                searchTerm,
                cancellationToken);
            if (hasNextResult.IsFailure)
            {
                return ToFailureAction<EmployeePageResponse>(hasNextResult);
            }
            hasNextPage = hasNextResult.Value;
        }

        return Ok(ApiResponse<EmployeePageResponse>.Ok(new EmployeePageResponse(
            MapEmployees(employees),
            hasPreviousPage,
            hasNextPage)));
    }

    [HttpGet("{employeeId:long}")]
    public async Task<ActionResult<ApiResponse<EmployeeResponse>>> GetByIdAsync(
        long employeeId,
        CancellationToken cancellationToken = default)
    {
        var employeeResult = await employeeManagementUseCase.GetForEditAsync(employeeId, cancellationToken);
        if (employeeResult.IsFailure)
        {
            return ToFailureAction<EmployeeResponse>(employeeResult);
        }
        var employee = employeeResult.Value;

        return Ok(ApiResponse<EmployeeResponse>.Ok(new EmployeeResponse(
            employee.Id,
            employee.Ci.GetValueOrDefault(),
            employee.Complemento,
            employee.Nombres,
            employee.PrimerApellido,
            employee.SegundoApellido,
            employee.Cargo,
            employee.NumeroContacto)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> CreateAsync(
        SaveEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await employeeManagementUseCase.CreateAsync(
            new CreateEmployeeDto
            {
                Ci = request.Ci,
                Complemento = request.Complemento,
                Nombres = request.Nombres,
                PrimerApellido = request.PrimerApellido,
                SegundoApellido = request.SegundoApellido,
                Cargo = request.Cargo,
                NumeroContacto = request.NumeroContacto
            },
            BuildActor(),
            cancellationToken);

        if (result.IsFailure)
        {
            return ToFailureAction<bool>(result);
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPut("{employeeId:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateAsync(
        long employeeId,
        SaveEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await employeeManagementUseCase.UpdateAsync(
            new UpdateEmployeeDto
            {
                Id = employeeId,
                Ci = request.Ci,
                Complemento = request.Complemento,
                Nombres = request.Nombres,
                PrimerApellido = request.PrimerApellido,
                SegundoApellido = request.SegundoApellido,
                Cargo = request.Cargo,
                NumeroContacto = request.NumeroContacto
            },
            BuildActor(),
            cancellationToken);

        if (result.IsFailure)
        {
            return ToFailureAction<bool>(result);
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpDelete("{employeeId:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAsync(
        long employeeId,
        CancellationToken cancellationToken = default)
    {
        var result = await employeeManagementUseCase.DeleteAsync(employeeId, BuildActor(), cancellationToken);
        if (result.IsFailure)
        {
            return ToFailureAction<bool>(result);
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    private static IReadOnlyList<EmployeeResponse> MapEmployees(IReadOnlyList<EmployeeModel> employees)
    {
        return employees
            .Select(employee => new EmployeeResponse(
                employee.Id,
                employee.Ci,
                employee.Complemento,
                employee.Nombres,
                employee.PrimerApellido,
                employee.SegundoApellido,
                employee.Cargo,
                employee.NumeroContacto))
            .ToList();
    }

    private AuditActor BuildActor()
    {
        return new AuditActor
        {
            UserId = ResolveUserId(),
            Username = ResolveUsername(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
    }

    private long ResolveUserId()
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(userIdText, out var userId) && userId > 0)
        {
            return userId;
        }

        userIdText = Request.Headers["X-User-Id"].FirstOrDefault();
        if (long.TryParse(userIdText, out userId) && userId > 0)
        {
            return userId;
        }

        return 1;
    }

    private string ResolveUsername()
    {
        if (!string.IsNullOrWhiteSpace(User.Identity?.Name))
        {
            return User.Identity.Name;
        }

        var username = Request.Headers["X-Username"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        return "frontend";
    }

    private static ApiResponse<T> ToFailure<T>(Result result)
    {
        if (result.Errors.Count > 0)
        {
            return ApiResponse<T>.Fail(result.Errors);
        }

        return ApiResponse<T>.Fail(GetErrors(result).ToArray());
    }

    private static IReadOnlyList<string> GetErrors(Result result)
    {
        if (result.Errors.Count == 0)
        {
            return [result.ErrorMessage];
        }

        return result.Errors
            .SelectMany(error => error.Value)
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .DefaultIfEmpty(result.ErrorMessage)
            .ToList();
    }

    private static bool IsNotFoundFailure(Result result)
    {
        if (result.Errors.ContainsKey("NotFound"))
        {
            return true;
        }

        return result.ErrorMessage.Contains("no encontrado", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("no existe", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidationFailure(Result result)
    {
        if (result.Errors.Count > 0)
        {
            return true;
        }

        return result.ErrorMessage.Contains("oblig", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("valid", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("invál", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("inval", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("formato", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("debe", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("requer", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("existe", StringComparison.OrdinalIgnoreCase);
    }

    private ActionResult<ApiResponse<T>> ToFailureAction<T>(Result result)
    {
        var payload = ToFailure<T>(result);
        if (IsNotFoundFailure(result))
        {
            return NotFound(payload);
        }

        if (IsValidationFailure(result))
        {
            return BadRequest(payload);
        }

        return StatusCode(StatusCodes.Status500InternalServerError, payload);
    }
}
