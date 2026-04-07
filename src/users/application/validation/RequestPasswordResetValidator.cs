using Mercadito.src.users.application.models;
using Shared.Domain;
using System.Text.RegularExpressions;

namespace Mercadito.src.users.application.validation
{
    public sealed class RequestPasswordResetValidator : IRequestPasswordResetValidator
    {
        private const string UsernamePattern = "^[a-z0-9._-]{4,40}$";
        private const string EmailPattern = "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$";
        private readonly Dictionary<string, List<string>> _errors = new();

        public Result<RequestPasswordResetDto> Validate(RequestPasswordResetDto input)
        {
            if (input == null)
            {
                return Result<RequestPasswordResetDto>.Failure("La solicitud es obligatoria.");
            }

            _errors.Clear();
            var normalized = Normalize(input);

            ValidateIdentifier(normalized.Identifier);
            ValidateResetUrlBase(normalized.ResetUrlBase);

            return _errors.Count > 0
                ? Result<RequestPasswordResetDto>.Failure(_errors)
                : Result<RequestPasswordResetDto>.Success(normalized);
        }

        private void ValidateIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                UserValidationHelpers.AddError(_errors, "Identifier", "El usuario o correo es obligatorio.");
                return;
            }

            if (identifier.Contains('@'))
            {
                if (identifier.Length > 100)
                {
                    UserValidationHelpers.AddError(_errors, "Identifier", "El correo no puede exceder 100 caracteres.");
                }

                if (!Regex.IsMatch(identifier, EmailPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
                {
                    UserValidationHelpers.AddError(_errors, "Identifier", "Ingresa un correo válido.");
                }

                return;
            }

            if (identifier.Length < 4 || identifier.Length > 40)
            {
                UserValidationHelpers.AddError(_errors, "Identifier", "El usuario debe tener entre 4 y 40 caracteres.");
            }

            if (!Regex.IsMatch(identifier, UsernamePattern, RegexOptions.CultureInvariant))
            {
                UserValidationHelpers.AddError(_errors, "Identifier", "El usuario solo admite minúsculas, números, punto, guion y guion bajo.");
            }
        }

        private void ValidateResetUrlBase(string resetUrlBase)
        {
            if (string.IsNullOrWhiteSpace(resetUrlBase))
            {
                UserValidationHelpers.AddError(_errors, "ResetUrlBase", "La URL de restablecimiento es obligatoria.");
                return;
            }

            if (!Uri.TryCreate(resetUrlBase, UriKind.Absolute, out _))
            {
                UserValidationHelpers.AddError(_errors, "ResetUrlBase", "La URL de restablecimiento es inválida.");
            }
        }

        private static RequestPasswordResetDto Normalize(RequestPasswordResetDto input)
        {
            var normalizedIdentifier = UserValidationHelpers.NormalizeCollapsed(input.Identifier);

            return new RequestPasswordResetDto
            {
                Identifier = normalizedIdentifier.ToLowerInvariant(),
                ResetUrlBase = UserValidationHelpers.NormalizeCollapsed(input.ResetUrlBase)
            };
        }
    }
}
