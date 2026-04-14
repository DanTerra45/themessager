using Mercadito.src.application.users.models;
using Mercadito.src.application.users.ports.input;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mercadito.Pages.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel(IRequestPasswordResetUseCase requestPasswordResetUseCase) : AppPageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "El usuario o correo es obligatorio.")]
        public string Identifier { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAsync()
        {
            var resetRequest = new RequestPasswordResetDto
            {
                Identifier = Identifier,
                ResetUrlBase = BuildResetUrlBase()
            };

            var result = await requestPasswordResetUseCase.ExecuteAsync(resetRequest, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                ApplyResultErrors(result);

                if (result.Errors.Count == 0)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                }

                return Page();
            }

            TempData["SuccessMessage"] = "Si la cuenta existe y tiene un correo asociado, se envió un enlace de restablecimiento.";
            return LocalRedirect("/Login");
        }

        private string BuildResetUrlBase()
        {
            return BuildAbsolutePathUrl("/ResetPassword").ToString();
        }
    }
}
