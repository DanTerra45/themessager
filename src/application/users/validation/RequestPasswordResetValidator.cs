using Mercadito.src.domain.shared.validation;
using Mercadito.src.application.users.models;
using Mercadito.src.domain.shared;
using System.Text.RegularExpressions;

namespace Mercadito.src.application.users.validation
{
    public sealed class RequestPasswordResetValidator : IRequestPasswordResetValidator
    {
        private const string UsernamePattern = "^[a-z0-9._-]{4,40}$";
        private const string EmailPattern = "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$";
        private readonly ValidationErrorBag _errors = new();
        private readonly StringRuleSet _emailIdentifierRules = new();
        private readonly StringRuleSet _usernameIdentifierRules = new();
        private readonly StringRuleSet _stringRules = new();

        public RequestPasswordResetValidator()
        {
            _emailIdentifierRules.Add("Identifier", StringValidationRules.MaxLength(100, "El correo no puede exceder 100 caracteres."));
            _emailIdentifierRules.Add("Identifier", StringValidationRules.RegexMatch(EmailPattern, "Ingresa un correo válido.", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase));

            _usernameIdentifierRules.Add("Identifier", StringValidationRules.LengthBetween(4, 40, "El usuario debe tener entre 4 y 40 caracteres."));
            _usernameIdentifierRules.Add("Identifier", StringValidationRules.RegexMatch(UsernamePattern, "El usuario solo admite minúsculas, números, punto, guion y guion bajo."));

            _stringRules.Add("ResetUrlBase", StringValidationRules.Required("La URL de restablecimiento es obligatoria."));
            _stringRules.Add("ResetUrlBase", StringValidationRules.AbsoluteUri("La URL de restablecimiento es inválida."));
        }

        public Result<RequestPasswordResetDto> Validate(RequestPasswordResetDto input)
        {
            if (input == null)
            {
                return Result.Failure<RequestPasswordResetDto>("La solicitud es obligatoria.");
            }

            _errors.Clear();
            var normalized = Normalize(input);

            ValidateIdentifier(normalized.Identifier);
            _stringRules.Validate("ResetUrlBase", normalized.ResetUrlBase, _errors);

            if (_errors.HasErrors)
            {
                return Result.Failure<RequestPasswordResetDto>(_errors.ToDictionary());
            }

            return Result.Success(normalized);
        }

        private void ValidateIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                _errors.Add("Identifier", "El usuario o correo es obligatorio.");
                return;
            }

            if (identifier.Contains('@', StringComparison.Ordinal))
            {
                _emailIdentifierRules.Validate("Identifier", identifier, _errors);
                return;
            }

            _usernameIdentifierRules.Validate("Identifier", identifier, _errors);
        }

        private static RequestPasswordResetDto Normalize(RequestPasswordResetDto input)
        {
            var normalizedIdentifier = ValidationText.NormalizeTrimmed(input.Identifier);

            return new RequestPasswordResetDto
            {
                Identifier = ValidationText.NormalizeLowerTrimmed(normalizedIdentifier),
                ResetUrlBase = ValidationText.NormalizeTrimmed(input.ResetUrlBase)
            };
        }
    }
}
