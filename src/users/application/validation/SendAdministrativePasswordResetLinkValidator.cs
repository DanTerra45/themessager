using Mercadito.src.shared.domain;
using Mercadito.src.shared.domain.validation;
using Mercadito.src.users.application.models;
using System.Text.RegularExpressions;

namespace Mercadito.src.users.application.validation
{
    public sealed class SendAdministrativePasswordResetLinkValidator : ISendAdministrativePasswordResetLinkValidator
    {
        private const string EmailPattern = "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$";
        private readonly ValidationErrorBag _errors = new();
        private readonly StringRuleSet _stringRules = new();

        public SendAdministrativePasswordResetLinkValidator()
        {
            _stringRules.Add("Username", StringValidationRules.Required("El usuario es obligatorio."));
            _stringRules.Add("Email", StringValidationRules.Required("El correo es obligatorio."));
            _stringRules.Add("Email", StringValidationRules.MaxLength(100, "El correo no puede exceder 100 caracteres."));
            _stringRules.Add("Email", StringValidationRules.RegexMatch(EmailPattern, "El correo no tiene un formato válido.", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase));
            _stringRules.Add("ResetUrlBase", StringValidationRules.Required("La URL de restablecimiento es obligatoria."));
            _stringRules.Add("ResetUrlBase", StringValidationRules.AbsoluteUri("La URL de restablecimiento es inválida."));
        }

        public Result<SendAdministrativePasswordResetLinkDto> Validate(SendAdministrativePasswordResetLinkDto input)
        {
            if (input == null)
            {
                return Result.Failure<SendAdministrativePasswordResetLinkDto>("La solicitud es obligatoria.");
            }

            _errors.Clear();
            var normalized = Normalize(input);

            if (normalized.UserId <= 0)
            {
                _errors.Add("UserId", "El usuario es inválido.");
            }

            _stringRules.Validate("Username", normalized.Username, _errors);
            _stringRules.Validate("Email", normalized.Email, _errors);
            _stringRules.Validate("ResetUrlBase", normalized.ResetUrlBase, _errors);

            if (_errors.HasErrors)
            {
                return Result.Failure<SendAdministrativePasswordResetLinkDto>(_errors.ToDictionary());
            }

            return Result.Success(normalized);
        }

        private static SendAdministrativePasswordResetLinkDto Normalize(SendAdministrativePasswordResetLinkDto input)
        {
            return new SendAdministrativePasswordResetLinkDto
            {
                UserId = input.UserId,
                Username = ValidationText.NormalizeLowerTrimmed(input.Username),
                Email = ValidationText.NormalizeTrimmed(input.Email),
                ResetUrlBase = ValidationText.NormalizeTrimmed(input.ResetUrlBase)
            };
        }
    }
}
