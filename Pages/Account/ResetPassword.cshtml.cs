using Mercadito.src.application.users.models;
using Mercadito.src.application.users.ports.input;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mercadito.Pages.Infrastructure;
using Mercadito.src.domain.shared.validation;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel(
        IValidatePasswordResetTokenUseCase validatePasswordResetTokenUseCase,
        ICompletePasswordResetUseCase completePasswordResetUseCase) : AppPageModel
    {
        [BindProperty(SupportsGet = true)]
        public string Token { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 128 caracteres.")]
        [RegularExpression("^(?=.*[A-Z])(?=.*[a-z])(?=.*\\d).+$", ErrorMessage = "La contraseña debe incluir al menos una letra mayúscula, una minúscula y un número.")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [Compare(nameof(Password), ErrorMessage = "La confirmación no coincide con la contraseña.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Username { get; private set; } = string.Empty;
        public bool IsTokenValid { get; private set; }

        public async Task<IActionResult> OnGetAsync(string? token = null)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                Token = ValidationText.NormalizeTrimmed(token);
            }

            await LoadTokenStateAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var result = await completePasswordResetUseCase.ExecuteAsync(new CompletePasswordResetDto
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
            var result = await validatePasswordResetTokenUseCase.ExecuteAsync(Token, HttpContext.RequestAborted);
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
