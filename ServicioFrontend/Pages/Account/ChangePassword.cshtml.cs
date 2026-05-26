using ServicioFrontend.Adapters.Users;
using ServicioFrontend.Authentication;
using ServicioFrontend.Dtos.Users;
using ServicioFrontend.Pages.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ServicioFrontend.Pages.Account;

[Authorize]
public sealed class ChangePasswordModel(IUsersApiAdapter usersApiAdapter) : FrontendPageModel
{
    [BindProperty]
    public ChangePasswordRequestDto PasswordChange { get; set; } = new();

    public string Username { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public bool IsForcedChange { get; private set; }

    public IActionResult OnGet()
    {
        Username = ResolveUsername();
        Role = ResolveRole();
        IsForcedChange = RequiresForcedPasswordChange();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Username = ResolveUsername();
        Role = ResolveRole();
        IsForcedChange = RequiresForcedPasswordChange();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = ResolveUserId();
        if (userId <= 0)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return LocalRedirect("/Login");
        }

        var result = await usersApiAdapter.ChangePasswordAsync(
            PasswordChange,
            HttpContext.RequestAborted);

        if (!result.Success)
        {
            ApplyApiErrors(result, nameof(PasswordChange));
            TempData["ErrorMessage"] = FirstErrorOrDefault(result, "No se pudo actualizar la contraseña.");
            return Page();
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "Contraseña actualizada. Inicia sesión nuevamente.";
        return LocalRedirect("/Login");
    }

    private bool RequiresForcedPasswordChange()
    {
        var mustChangePasswordClaim = User.FindFirst(FrontendUserClaimTypes.MustChangePassword);
        if (mustChangePasswordClaim == null)
        {
            return false;
        }

        return string.Equals(mustChangePasswordClaim.Value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private string ResolveUsername()
    {
        return User.Identity?.Name ?? string.Empty;
    }

    private string ResolveRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}
