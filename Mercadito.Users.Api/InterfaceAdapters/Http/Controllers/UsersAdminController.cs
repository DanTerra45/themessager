using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Application.Users.Ports.Input;
using Mercadito.Users.Api.Application.Users.Ports.Output;
using Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Common;
using Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Users;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Users.Api.InterfaceAdapters.Http.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersAdminController(
    IGetAllUsersUseCase getAllUsersUseCase,
    IGetAvailableEmployeesUseCase getAvailableEmployeesUseCase,
    IRegisterUserUseCase registerUserUseCase,
    ISendAdministrativePasswordResetLinkUseCase sendResetLinkUseCase,
    IAssignTemporaryPasswordUseCase assignTemporaryPasswordUseCase,
    IDeactivateUserUseCase deactivateUserUseCase,
    IUserRepository userRepository) : UserApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserSummaryResponse>>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await getAllUsersUseCase.ExecuteAsync(cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(ToFailure<IReadOnlyList<UserSummaryResponse>>(result));
        }

        return Ok(ApiResponse<IReadOnlyList<UserSummaryResponse>>.Ok(MapUsers(result.Value)));
    }

    [HttpGet("available-employees")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AvailableEmployeeResponse>>>> GetAvailableEmployeesAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await getAvailableEmployeesUseCase.ExecuteAsync(cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(ToFailure<IReadOnlyList<AvailableEmployeeResponse>>(result));
        }

        return Ok(ApiResponse<IReadOnlyList<AvailableEmployeeResponse>>.Ok(MapAvailableEmployees(result.Value)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RegisterUserResponse>>> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await registerUserUseCase.ExecuteAsync(
            new CreateUserDto
            {
                Email = request.Email,
                EmployeeId = request.EmployeeId,
                Role = request.Role,
                SetupUrlBase = request.SetupUrlBase
            },
            BuildActor(),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ToFailure<RegisterUserResponse>(result));
        }

        return CreatedAtAction(
            actionName: nameof(GetAllAsync),
            value: ApiResponse<RegisterUserResponse>.Ok(new RegisterUserResponse(result.Value)));
    }

    [HttpPost("{userId:long}/send-reset-link")]
    public async Task<ActionResult<ApiResponse<bool>>> SendResetLinkAsync(
        long userId,
        SendPasswordResetLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetActiveByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return NotFound(ApiResponse<bool>.Fail("El usuario seleccionado no existe o no está activo."));
        }

        var result = await sendResetLinkUseCase.ExecuteAsync(
            new SendAdministrativePasswordResetLinkDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email ?? string.Empty,
                ResetUrlBase = request.ResetUrlBase
            },
            BuildActor(),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ToFailure<bool>(result));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("{userId:long}/temporary-password")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignTemporaryPasswordAsync(
        long userId,
        AssignTemporaryPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetActiveByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return NotFound(ApiResponse<bool>.Fail("El usuario seleccionado no existe o no está activo."));
        }

        var result = await assignTemporaryPasswordUseCase.ExecuteAsync(
            new AssignTemporaryPasswordDto
            {
                UserId = user.Id,
                Username = user.Username,
                Password = request.TemporaryPassword,
                ConfirmPassword = request.ConfirmTemporaryPassword
            },
            BuildActor(),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ToFailure<bool>(result));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("{userId:long}/deactivate")]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        var result = await deactivateUserUseCase.ExecuteAsync(userId, BuildActor(), cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(ToFailure<bool>(result));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    private static IReadOnlyList<UserSummaryResponse> MapUsers(IReadOnlyList<UserListItem> users)
    {
        return users
            .Select(user => new UserSummaryResponse(
                user.Id,
                user.Username,
                user.Email ?? string.Empty,
                user.Role.ToString(),
                user.State,
                user.EmployeeId,
                user.EmployeeFullName,
                user.EmployeeCargo,
                string.Equals(user.State, "A", StringComparison.OrdinalIgnoreCase),
                user.CreatedAt,
                user.LastLogin))
            .ToList();
    }

    private static IReadOnlyList<AvailableEmployeeResponse> MapAvailableEmployees(IReadOnlyList<AvailableEmployeeOption> employees)
    {
        return employees
            .Select(employee => new AvailableEmployeeResponse(
                employee.Id,
                employee.FullName,
                employee.Cargo,
                employee.CiDisplay))
            .ToList();
    }
}
