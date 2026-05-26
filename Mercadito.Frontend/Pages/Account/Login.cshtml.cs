using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Mercadito.Frontend.Adapters.Users;
using Mercadito.Frontend.Authentication;
using Mercadito.Frontend.Dtos.Users;
using Mercadito.Frontend.Pages.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Frontend.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel(IUsersApiAdapter usersApiAdapter) : FrontendPageModel
{
    [BindProperty(SupportsGet = true, Name = "email_or_username")]
    [Required(ErrorMessage = "El email o nombre de usuario es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string EmailOrUsername { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = "/";

    public IActionResult OnGet(string? returnUrl = null)
    {
        ReturnUrl = NormalizeReturnUrl(returnUrl);
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(ReturnUrl);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = NormalizeReturnUrl(ReturnUrl);

        var result = await usersApiAdapter.LoginAsync(
            new LoginRequestDto(EmailOrUsername, Password),
            HttpContext.RequestAborted);

        if (!result.Success || result.Data == null)
        {
            ApplyApiErrors(result);

            if (result.ValidationErrors.Count == 0)
            {
                TempData["ErrorMessage"] = FirstErrorOrDefault(result, "No se pudo iniciar sesión.");
            }

            return Page();
        }

        var user = result.Data;
        var claims = BuildClaimsFromAccessToken(user.AccessToken);

        if (user.NeedChangePassword)
        {
            claims.Add(new Claim(FrontendUserClaimTypes.MustChangePassword, "true"));
        }

        claims.Add(new Claim(FrontendUserClaimTypes.AccessToken, user.AccessToken));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        TempData["SuccessMessage"] = "Sesión iniciada.";
        if (user.NeedChangePassword)
        {
            return LocalRedirect("/ChangePassword");
        }

        return LocalRedirect(ReturnUrl);
    }

    public async Task<IActionResult> OnPostSignOutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "Sesión cerrada.";
        return LocalRedirect("/");
    }

    private static string NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        return returnUrl.StartsWith('/') ? returnUrl : "/";
    }

    private static List<Claim> BuildClaimsFromAccessToken(string accessToken)
    {
        var payload = ParseJwtPayload(accessToken);
        var sub = GetClaimValue(payload, "sub");
        var nickname = GetClaimValue(payload, "nickname");
        var email = GetClaimValue(payload, "email");
        var role = GetClaimValue(payload, "role");

        var claims = new List<Claim>();

        if (!string.IsNullOrWhiteSpace(sub) && long.TryParse(sub, NumberStyles.None, CultureInfo.InvariantCulture, out var userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)));
        }

        if (!string.IsNullOrWhiteSpace(nickname))
        {
            claims.Add(new Claim(ClaimTypes.Name, nickname));
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Name, email));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        return claims;
    }

    private static JsonElement ParseJwtPayload(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length < 2)
        {
            return default;
        }

        var payloadBytes = Base64UrlDecode(parts[1]);
        using var document = JsonDocument.Parse(payloadBytes);
        return document.RootElement.Clone();
    }

    private static string? GetClaimValue(JsonElement payload, string claimName)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!payload.TryGetProperty(claimName, out var claimValue))
        {
            return null;
        }

        return claimValue.ValueKind switch
        {
            JsonValueKind.String => claimValue.GetString(),
            JsonValueKind.Number => claimValue.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => claimValue.GetRawText()
        };
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }
}
