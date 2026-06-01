using ServicioFrontend.Adapters.Users;
using ServicioFrontend.Dtos.Users;
using ServicioFrontend.Pages.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServicioFrontend.Pages.Account;

[AllowAnonymous]
public sealed class ForgotPasswordModel(IUsersApiAdapter usersApiAdapter) : FrontendPageModel
{
    [BindProperty]
    public RequestPasswordResetRequestDto PasswordReset { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await usersApiAdapter.RequestPasswordResetAsync(PasswordReset, HttpContext.RequestAborted);
        if (!result.Success)
        {
            ApplyApiErrors(result, nameof(PasswordReset));

            if (result.ValidationErrors.Count == 0)
            {
                TempData["ErrorMessage"] = FirstErrorOrDefault(result, "No se pudo solicitar el restablecimiento.");
            }

            return Page();
        }

        TempData["SuccessMessage"] = "Si la cuenta existe y tiene un correo asociado, se envió un enlace de restablecimiento.";
        return LocalRedirect("/Login");
    }
}
