using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Application.Users.Ports.Input;
using Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Common;
using Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Users;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Users.Api.InterfaceAdapters.Http.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersAuthController(
    IAuthenticateUserUseCase authenticateUserUseCase,
    IRequestPasswordResetUseCase requestPasswordResetUseCase,
    IValidatePasswordResetTokenUseCase validatePasswordResetTokenUseCase,
    ICompletePasswordResetUseCase completePasswordResetUseCase,
    IForcePasswordChangeUseCase forcePasswordChangeUseCase) : UserApiControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authenticateUserUseCase.ExecuteAsync(
            new LoginUserCommand
            {
                Username = request.UserName,
                Password = request.Password
            },
            cancellationToken);

        if (result.IsFailure)
        {
            return Unauthorized(ToFailure<LoginResponse>(result));
        }

        return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse(
            result.Value.Id,
            result.Value.Username,
            result.Value.Role.ToString(),
            result.Value.EmployeeId,
            result.Value.MustChangePassword,
            result.Value.LastLogin)));
    }

    [HttpPost("password-reset/request")]
    public async Task<ActionResult<ApiResponse<bool>>> RequestPasswordResetAsync(
        RequestPasswordResetRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await requestPasswordResetUseCase.ExecuteAsync(
            new RequestPasswordResetDto
            {
                Identifier = request.Identifier,
                ResetUrlBase = request.ResetUrlBase
            },
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ToFailure<bool>(result));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpGet("password-reset/{token}")]
    public async Task<ActionResult<ApiResponse<PasswordResetTokenResponse>>> ValidatePasswordResetTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var result = await validatePasswordResetTokenUseCase.ExecuteAsync(token, cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ToFailure<PasswordResetTokenResponse>(result));
        }

        return Ok(ApiResponse<PasswordResetTokenResponse>.Ok(new PasswordResetTokenResponse(
            result.Value.UserId,
            result.Value.Username,
            result.Value.Email,
            result.Value.ExpiresAtUtc)));
    }

    [HttpPost("password-reset/complete")]
    public async Task<ActionResult<ApiResponse<bool>>> CompletePasswordResetAsync(
        CompletePasswordResetRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await completePasswordResetUseCase.ExecuteAsync(
            new CompletePasswordResetDto
            {
                Token = request.Token,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword
            },
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ToFailure<bool>(result));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("{userId:long}/force-password-change")]
    public async Task<ActionResult<ApiResponse<bool>>> ForcePasswordChangeAsync(
        long userId,
        ForcePasswordChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await forcePasswordChangeUseCase.ExecuteAsync(
            userId,
            new ForcePasswordChangeDto
            {
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword
            },
            BuildActor(),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ToFailure<bool>(result));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
