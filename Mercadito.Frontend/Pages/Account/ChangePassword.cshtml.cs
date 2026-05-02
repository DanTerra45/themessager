using Mercadito.Frontend.Adapters.Users;
using Mercadito.Frontend.Authentication;
using Mercadito.Frontend.Dtos.Users;
using Mercadito.Frontend.Pages.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Frontend.Pages.Account;

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
        TempData["SuccessMessage"] = "La contraseña fue actualizada. Inicia sesión con la nueva credencial.";
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
