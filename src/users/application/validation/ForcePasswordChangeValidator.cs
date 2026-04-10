using Mercadito.src.shared.domain;
using Mercadito.src.shared.domain.validation;
using Mercadito.src.users.application.models;

namespace Mercadito.src.users.application.validation
{
    public sealed class ForcePasswordChangeValidator : IForcePasswordChangeValidator
    {
        private readonly ValidationErrorBag _errors = new();

        public Result<ForcePasswordChangeDto> Validate(ForcePasswordChangeDto input)
        {
            if (input == null)
            {
                return Result.Failure<ForcePasswordChangeDto>("La solicitud es obligatoria.");
            }

            _errors.Clear();
            var normalized = Normalize(input);

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
                return Result.Failure<ForcePasswordChangeDto>(_errors.ToDictionary());
            }

            return Result.Success(normalized);
        }

        private static ForcePasswordChangeDto Normalize(ForcePasswordChangeDto input)
        {
            return new ForcePasswordChangeDto
            {
                Password = input.Password,
                ConfirmPassword = input.ConfirmPassword
            };
        }
    }
}
