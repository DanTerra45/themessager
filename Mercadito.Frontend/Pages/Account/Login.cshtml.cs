using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
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
    [BindProperty]
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    public string Username { get; set; } = string.Empty;

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
            new LoginRequestDto(Username, Password),
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
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role)
        };

        if (user.EmployeeId.HasValue)
        {
            claims.Add(new Claim("employee_id", user.EmployeeId.Value.ToString(CultureInfo.InvariantCulture)));
        }

        if (user.MustChangePassword)
        {
            claims.Add(new Claim(FrontendUserClaimTypes.MustChangePassword, "true"));
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        TempData["SuccessMessage"] = "Sesión iniciada.";
        if (user.MustChangePassword)
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
}
