using Application.Auth;
using Application.Common;
using Application.UseCases;
using Application.Utils;
using Domain.Common;
using Domain.Dto.Auth;
using Domain.Dto.Jwt;
using Domain.Dto.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controller;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginUseCase _loginUseCase;
    private readonly RequestPasswordResetUseCase _requestPasswordResetUseCase;
    private readonly ResetPasswordUseCase _resetPasswordUseCase;

    public AuthController(
        LoginUseCase loginUseCase,
        RequestPasswordResetUseCase requestPasswordResetUseCase,
        ResetPasswordUseCase resetPasswordUseCase)
    {
        _loginUseCase = loginUseCase;
        _requestPasswordResetUseCase = requestPasswordResetUseCase;
        _resetPasswordUseCase = resetPasswordUseCase;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
    {
        var result = await _loginUseCase.Execute(request);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }
        Response.Cookies.Append("access_token", result.Value.accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(60),
            Path = "/"
        });
        return this.ToActionResult(result, StatusCodes.Status200OK);
    }
    [Authorize]
    [HttpPut("reset-password")]
    public async Task<IActionResult> ResetPassword()
    {
        var userIdValue = User.FindFirst("sub")?.Value;
        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new { message = "Invalid token or missing user id." });
        }

        var result = await _requestPasswordResetUseCase.Execute(userId);
        return this.ToActionResult(result, StatusCodes.Status200OK);
    }

    [AllowAnonymous]
    [HttpPost("reset-password/confirm")]
    public async Task<IActionResult> ConfirmResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _resetPasswordUseCase.Execute(request);
        return this.ToActionResult(result, StatusCodes.Status200OK);
    }

    [AllowAnonymous]
    [HttpGet("generate-password")]
    public IActionResult GeneratePassword()
    {
        var (password , hash) = PasswordUtils.GenerateSecurePassword(20);
        return Ok(new {password = password, hash = hash });
    }
}