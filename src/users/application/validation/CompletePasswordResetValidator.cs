using Mercadito.src.shared.domain.validation;
using Mercadito.src.users.application.models;
using Mercadito.src.shared.domain;

namespace Mercadito.src.users.application.validation
{
    public sealed class CompletePasswordResetValidator : ICompletePasswordResetValidator
    {
        private readonly ValidationErrorBag _errors = new();

        public Result<CompletePasswordResetDto> Validate(CompletePasswordResetDto input)
        {
            if (input == null)
            {
                return Result.Failure<CompletePasswordResetDto>("La solicitud es obligatoria.");
            }

            _errors.Clear();
            var normalized = Normalize(input);

            if (string.IsNullOrWhiteSpace(normalized.Token))
            {
                _errors.Add("Token", "El token de restablecimiento es obligatorio.");
            }
            else if (normalized.Token.Length > 256)
            {
                _errors.Add("Token", "El token de restablecimiento es inválido.");
            }

            if (string.IsNullOrWhiteSpace(normalized.Password))
            {
                _errors.Add("Password", "La contraseña es obligatoria.");
            }
            else
            {
                UserPasswordRules.AddPasswordErrors(_errors, "Password", normalized.Password);
            }

            if (string.IsNullOrWhiteSpace(normalized.ConfirmPassword))
            {
                _errors.Add("ConfirmPassword", "La confirmación de contraseña es obligatoria.");
            }
            else if (!string.Equals(normalized.Password, normalized.ConfirmPassword, StringComparison.Ordinal))
            {
                _errors.Add("ConfirmPassword", "La confirmación no coincide con la contraseña.");
            }

            if (_errors.HasErrors)
            {
                return Result.Failure<CompletePasswordResetDto>(_errors.ToDictionary());
            }

            return Result.Success(normalized);
        }

        private static CompletePasswordResetDto Normalize(CompletePasswordResetDto input)
        {
            return new CompletePasswordResetDto
            {
                Token = ValidationText.NormalizeTrimmed(input.Token),
                Password = input.Password,
                ConfirmPassword = input.ConfirmPassword
            };
        }
    }
}
