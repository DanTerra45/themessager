using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mercadito.Pages.Infrastructure;

namespace Mercadito.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : AppPageModel
    {
        private readonly IRequestPasswordResetUseCase _requestPasswordResetUseCase;

        public ForgotPasswordModel(IRequestPasswordResetUseCase requestPasswordResetUseCase)
        {
            _requestPasswordResetUseCase = requestPasswordResetUseCase;
        }

        [BindProperty]
        public string Identifier { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAsync()
        {
            var resetRequest = new RequestPasswordResetDto
            {
                Identifier = Identifier,
                ResetUrlBase = BuildResetUrlBase()
            };

            var result = await _requestPasswordResetUseCase.ExecuteAsync(resetRequest, HttpContext.RequestAborted);
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
            return BuildAbsolutePathUrl("/ResetPassword");
        }
    }
}
