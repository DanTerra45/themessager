using ServicioFrontend.Adapters.Users;
using ServicioFrontend.Authentication;
using ServicioFrontend.Dtos.Users;
using ServicioFrontend.Pages.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServicioFrontend.Pages.Account;

[Authorize]
public sealed class ChangePasswordModel(IUsersApiAdapter usersApiAdapter) : FrontendPageModel
{
    [BindProperty]
    public ForcePasswordChangeRequestDto PasswordChange { get; set; } = new();

    public string Username { get; private set; } = string.Empty;

    public IActionResult OnGet()
    {
        Username = ResolveUsername();

        if (!RequiresForcedPasswordChange())
        {
            return LocalRedirect("/");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Username = ResolveUsername();

        if (!RequiresForcedPasswordChange())
        {
            return LocalRedirect("/");
        }

        var userId = ResolveUserId();
        if (userId <= 0)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return LocalRedirect("/Login");
        }

        var result = await usersApiAdapter.ForcePasswordChangeAsync(
            userId,
            PasswordChange,
            BuildActorContext(),
            HttpContext.RequestAborted);

        if (!result.Success)
        {
            ApplyApiErrors(result, nameof(PasswordChange));
            return Page();
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "Se envió un enlace a tu correo para completar el cambio de contraseña.";
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
}
