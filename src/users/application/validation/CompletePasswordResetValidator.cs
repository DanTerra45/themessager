using Mercadito.src.users.application.models;
using Shared.Domain;

namespace Mercadito.src.users.application.validation
{
    public sealed class CompletePasswordResetValidator : ICompletePasswordResetValidator
    {
        private readonly Dictionary<string, List<string>> _errors = new();

        public Result<CompletePasswordResetDto> Validate(CompletePasswordResetDto input)
        {
            if (input == null)
            {
                return Result<CompletePasswordResetDto>.Failure("La solicitud es obligatoria.");
            }

            _errors.Clear();
            var normalized = Normalize(input);

            if (string.IsNullOrWhiteSpace(normalized.Token))
            {
                UserValidationHelpers.AddError(_errors, "Token", "El token de restablecimiento es obligatorio.");
            }
            else if (normalized.Token.Length > 256)
            {
                UserValidationHelpers.AddError(_errors, "Token", "El token de restablecimiento es inválido.");
            }

            if (string.IsNullOrWhiteSpace(normalized.Password))
            {
                UserValidationHelpers.AddError(_errors, "Password", "La contraseña es obligatoria.");
            }
            else
            {
                UserPasswordRules.AddPasswordErrors(_errors, "Password", normalized.Password);
            }

            if (string.IsNullOrWhiteSpace(normalized.ConfirmPassword))
            {
                UserValidationHelpers.AddError(_errors, "ConfirmPassword", "La confirmación de contraseña es obligatoria.");
            }
            else if (!string.Equals(normalized.Password, normalized.ConfirmPassword, StringComparison.Ordinal))
            {
                UserValidationHelpers.AddError(_errors, "ConfirmPassword", "La confirmación no coincide con la contraseña.");
            }

            return _errors.Count > 0
                ? Result<CompletePasswordResetDto>.Failure(_errors)
                : Result<CompletePasswordResetDto>.Success(normalized);
        }

        private static CompletePasswordResetDto Normalize(CompletePasswordResetDto input)
        {
            return new CompletePasswordResetDto
            {
                Token = UserValidationHelpers.NormalizeCollapsed(input.Token),
                Password = input.Password,
                ConfirmPassword = input.ConfirmPassword
            };
        }
    }
}
