using ServicioFrontend.Adapters.Users;
using ServicioFrontend.Dtos.Users;
using ServicioFrontend.Pages.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServicioFrontend.Pages.Account;

[AllowAnonymous]
public sealed class ResetPasswordModel(IUsersApiAdapter usersApiAdapter) : FrontendPageModel
{
    [BindProperty]
    public CompletePasswordResetRequestDto PasswordReset { get; set; } = new();

    public string Username { get; private set; } = string.Empty;
    public bool IsTokenValid { get; private set; }

    public IActionResult OnGet(string? token = null, string? username = null)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            PasswordReset.Token = token.Trim();
        }

        Username = username?.Trim() ?? string.Empty;
        IsTokenValid = !string.IsNullOrWhiteSpace(PasswordReset.Token);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        IsTokenValid = !string.IsNullOrWhiteSpace(PasswordReset.Token);

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
