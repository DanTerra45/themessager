using System.Security.Claims;
using Mercadito.Sales.Api.Contracts.Common;
using Mercadito.Sales.Api.Contracts.Employees;
using Mercadito.src.application.employees.models;
using Mercadito.src.application.employees.ports.input;
using Mercadito.src.domain.audit.entities;
using Mercadito.src.domain.shared;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Sales.Api.Controllers;

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
        IReadOnlyList<EmployeeModel> employees = cursorEmployeeId > 0
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

        var hasPreviousPage = false;
        var hasNextPage = false;
        if (employees.Count > 0)
        {
            hasPreviousPage = await employeeManagementUseCase.HasEmployeesByCursorAsync(
                sortBy,
                sortDirection,
                employees[0].Id,
                isNextPage: false,
                searchTerm,
                cancellationToken);

            hasNextPage = await employeeManagementUseCase.HasEmployeesByCursorAsync(
                sortBy,
                sortDirection,
                employees[^1].Id,
                isNextPage: true,
                searchTerm,
                cancellationToken);
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
        var employee = await employeeManagementUseCase.GetForEditAsync(employeeId, cancellationToken);
        if (employee == null)
        {
            return NotFound(ApiResponse<EmployeeResponse>.Fail("Empleado no encontrado."));
        }

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
            return BadRequest(ToFailure<bool>(result));
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
            return BadRequest(ToFailure<bool>(result));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpDelete("{employeeId:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAsync(
        long employeeId,
        CancellationToken cancellationToken = default)
    {
        var wasDeleted = await employeeManagementUseCase.DeleteAsync(employeeId, BuildActor(), cancellationToken);
        if (!wasDeleted)
        {
            return NotFound(ApiResponse<bool>.Fail("El empleado no existe o ya estaba desactivado."));
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

        return ApiResponse<T>.Fail(result.ErrorMessage);
    }
}
