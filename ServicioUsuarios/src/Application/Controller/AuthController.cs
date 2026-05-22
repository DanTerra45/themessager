using Application.Auth;
using Application.Common;
using Application.UseCases;
using Application.utils;
using Domain.Common;
using Domain.Dto.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controller;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginUseCase _loginUseCase;

    public AuthController(LoginUseCase loginUseCase)
    {
        _loginUseCase = loginUseCase;
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
        Response.Cookies.Append("access_token", result.Value, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(60),
            Path = "/"
        });
        return this.ToActionResult(Result.Success(), StatusCodes.Status200OK);
    }
    [AllowAnonymous]
    [HttpGet("generate-password")]
    public IActionResult GeneratePassword()
    {
        var (password , hash) = PasswordUtils.GenerateSecurePassword(20);
        return Ok(new {password = password, hash = hash });
    }
}