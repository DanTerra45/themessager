using Mercadito.Frontend.Adapters.Users;
using Mercadito.Frontend.Dtos.Users;
using Mercadito.Frontend.Pages.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Frontend.Pages.Account;

[AllowAnonymous]
public sealed class ResetPasswordModel(IUsersApiAdapter usersApiAdapter) : FrontendPageModel
{
    [BindProperty]
    public CompletePasswordResetRequestDto PasswordReset { get; set; } = new();

    public string Username { get; private set; } = string.Empty;
    public bool IsTokenValid { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? token = null, string? username = null)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            PasswordReset.Token = token.Trim();
        }

        if (!string.IsNullOrWhiteSpace(username))
        {
            Username = username.Trim();
        }

        IsTokenValid = !string.IsNullOrWhiteSpace(PasswordReset.Token);

        if (!IsTokenValid)
        {
            Username = string.Empty;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await usersApiAdapter.CompletePasswordResetAsync(PasswordReset, HttpContext.RequestAborted);
        if (!result.Success)
        {
            ApplyApiErrors(result, nameof(PasswordReset));

            if (result.ValidationErrors.Count == 0)
            {
                TempData["ErrorMessage"] = FirstErrorOrDefault(result, "No se pudo actualizar la contraseña.");
            }

            return Page();
        }

        TempData["SuccessMessage"] = "La contraseña fue actualizada. Ya puedes iniciar sesión.";
        return LocalRedirect("/Login");
    }
}
