using Mercadito.src.users.application.models;
using Shared.Domain;

namespace Mercadito.src.users.application.validation
{
    public sealed class ResetUserPasswordValidator : IResetUserPasswordValidator
    {
        private readonly Dictionary<string, List<string>> _errors = new();

        public Result<ResetUserPasswordDto> Validate(ResetUserPasswordDto input)
        {
            if (input == null)
            {
                return Result<ResetUserPasswordDto>.Failure("La solicitud es obligatoria.");
            }

            _errors.Clear();
            var normalized = Normalize(input);

            if (normalized.UserId <= 0)
            {
                UserValidationHelpers.AddError(_errors, "UserId", "El usuario es inválido.");
            }

            if (string.IsNullOrWhiteSpace(normalized.Username))
            {
                UserValidationHelpers.AddError(_errors, "Username", "El usuario es obligatorio.");
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
                ? Result<ResetUserPasswordDto>.Failure(_errors)
                : Result<ResetUserPasswordDto>.Success(normalized);
        }

        private static ResetUserPasswordDto Normalize(ResetUserPasswordDto input)
        {
            return new ResetUserPasswordDto
            {
                UserId = input.UserId,
                Username = UserValidationHelpers.NormalizeCollapsed(input.Username),
                Password = input.Password,
                ConfirmPassword = input.ConfirmPassword
            };
        }
    }
}
