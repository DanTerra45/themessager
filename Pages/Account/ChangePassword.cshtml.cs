using System.Globalization;
using System.Security.Claims;
using Mercadito.Pages.Infrastructure;
using Mercadito.src.users.application;
using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Account
{
    [Authorize]
    public class ChangePasswordModel(IForcePasswordChangeUseCase forcePasswordChangeUseCase) : AppPageModel
    {
        [BindProperty]
        public ForcePasswordChangeDto PasswordChange { get; set; } = new();

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

            var actor = BuildAuditActor();
            var result = await forcePasswordChangeUseCase.ExecuteAsync(userId, PasswordChange, actor, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                ApplyResultErrors(result, "PasswordChange");
                return Page();
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "La contraseña fue actualizada. Inicia sesión con la nueva credencial.";
            return LocalRedirect("/Login");
        }

        private bool RequiresForcedPasswordChange()
        {
            var mustChangePasswordClaim = User.FindFirst(UserClaimTypes.MustChangePassword);
            if (mustChangePasswordClaim == null)
            {
                return false;
            }

            return string.Equals(mustChangePasswordClaim.Value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private long ResolveUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdValue))
            {
                return 0;
            }

            if (!long.TryParse(userIdValue, NumberStyles.None, CultureInfo.InvariantCulture, out var userId))
            {
                return 0;
            }

            return userId;
        }

        private string ResolveUsername()
        {
            if (User.Identity?.Name == null)
            {
                return string.Empty;
            }

            return User.Identity.Name;
        }
    }
}
