using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mercadito.Pages.Infrastructure;

namespace Mercadito.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : AppPageModel
    {
        private readonly IValidatePasswordResetTokenUseCase _validatePasswordResetTokenUseCase;
        private readonly ICompletePasswordResetUseCase _completePasswordResetUseCase;

        public ResetPasswordModel(
            IValidatePasswordResetTokenUseCase validatePasswordResetTokenUseCase,
            ICompletePasswordResetUseCase completePasswordResetUseCase)
        {
            _validatePasswordResetTokenUseCase = validatePasswordResetTokenUseCase;
            _completePasswordResetUseCase = completePasswordResetUseCase;
        }

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Username { get; private set; } = string.Empty;
        public bool IsTokenValid { get; private set; }

        public async Task<IActionResult> OnGetAsync(string? token = null)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                Token = token.Trim();
            }

            await LoadTokenStateAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var result = await _completePasswordResetUseCase.ExecuteAsync(new CompletePasswordResetDto
            {
                Token = Token,
                Password = Password,
                ConfirmPassword = ConfirmPassword
            }, HttpContext.RequestAborted);

            if (result.IsFailure)
            {
                ApplyResultErrors(result);

                if (result.Errors.Count == 0)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                }

                await LoadTokenStateAsync();
                return Page();
            }

            TempData["SuccessMessage"] = "La contraseña fue actualizada. Ya puedes iniciar sesión.";
            return LocalRedirect("/Login");
        }

        private async Task LoadTokenStateAsync()
        {
            var result = await _validatePasswordResetTokenUseCase.ExecuteAsync(Token, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                IsTokenValid = false;
                Username = string.Empty;
                TempData["ErrorMessage"] = result.ErrorMessage;
                return;
            }

            IsTokenValid = true;
            Username = result.Value.Username;
        }
    }
}
