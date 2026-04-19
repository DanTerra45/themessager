using Mercadito.src.domain.shared;
using Mercadito.src.domain.shared.validation;
using Mercadito.src.application.users.models;

namespace Mercadito.src.application.users.validation
{
    public sealed class AssignTemporaryPasswordValidator : IAssignTemporaryPasswordValidator
    {
        private readonly ValidationErrorBag _errors = new();

        public Result<AssignTemporaryPasswordDto> Validate(AssignTemporaryPasswordDto input)
        {
            if (input == null)
            {
                return Result.Failure<AssignTemporaryPasswordDto>("La solicitud es obligatoria.");
            }

            _errors.Clear();
            var normalized = Normalize(input);

            if (normalized.UserId <= 0)
            {
                _errors.Add("UserId", "El usuario es inválido.");
            }

            if (string.IsNullOrWhiteSpace(normalized.Username))
            {
                _errors.Add("Username", "El usuario es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(normalized.Password))
            {
                _errors.Add("Password", "La contraseña temporal es obligatoria.");
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
                return Result.Failure<AssignTemporaryPasswordDto>(_errors.ToDictionary());
            }

            return Result.Success(normalized);
        }

        private static AssignTemporaryPasswordDto Normalize(AssignTemporaryPasswordDto input)
        {
            return new AssignTemporaryPasswordDto
            {
                UserId = input.UserId,
                Username = ValidationText.NormalizeLowerTrimmed(input.Username),
                Password = input.Password,
                ConfirmPassword = input.ConfirmPassword
            };
        }
    }
}
