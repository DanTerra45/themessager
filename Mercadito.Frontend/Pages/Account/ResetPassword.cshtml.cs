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

    public async Task<IActionResult> OnGetAsync(string? token = null)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            PasswordReset.Token = token.Trim();
        }

        await LoadTokenStateAsync();
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

            await LoadTokenStateAsync();
            return Page();
        }

        TempData["SuccessMessage"] = "La contraseña fue actualizada. Ya puedes iniciar sesión.";
        return LocalRedirect("/Login");
    }

    private async Task LoadTokenStateAsync()
    {
        var result = await usersApiAdapter.ValidatePasswordResetTokenAsync(PasswordReset.Token, HttpContext.RequestAborted);
        if (!result.Success || result.Data == null)
        {
            IsTokenValid = false;
            Username = string.Empty;
            TempData["ErrorMessage"] = FirstErrorOrDefault(result, "El enlace de restablecimiento no es válido.");
            return;
        }

        IsTokenValid = true;
        Username = result.Data.UserName;
    }
}
