using Mercadito.src.users.application.models;
using Shared.Domain;
using System.Text.RegularExpressions;

namespace Mercadito.src.users.application.validation
{
    public sealed class LoginUserValidator : ILoginUserValidator
    {
        private const string UsernamePattern = "^[a-z0-9._-]{4,40}$";
        private readonly Dictionary<string, List<string>> _errors = new();

        public Result<LoginUserCommand> Validate(LoginUserCommand input)
        {
            if (input == null)
            {
                return Result<LoginUserCommand>.Failure("La solicitud es obligatoria.");
            }

            _errors.Clear();
            var normalized = Normalize(input);

            ValidateUsername(normalized.Username);
            ValidatePassword(normalized.Password);

            return _errors.Count > 0
                ? Result<LoginUserCommand>.Failure(_errors)
                : Result<LoginUserCommand>.Success(normalized);
        }

        private void ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                UserValidationHelpers.AddError(_errors, "Username", "El usuario es obligatorio.");
                return;
            }

            if (username.Length < 4 || username.Length > 40)
            {
                UserValidationHelpers.AddError(_errors, "Username", "El usuario debe tener entre 4 y 40 caracteres.");
            }

            if (!Regex.IsMatch(username, UsernamePattern, RegexOptions.CultureInvariant))
            {
                UserValidationHelpers.AddError(_errors, "Username", "El usuario solo admite minúsculas, números, punto, guion y guion bajo.");
            }
        }

        private void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                UserValidationHelpers.AddError(_errors, "Password", "La contraseña es obligatoria.");
                return;
            }

            if (password.Length > 128)
            {
                UserValidationHelpers.AddError(_errors, "Password", "La contraseña no puede exceder 128 caracteres.");
            }
        }

        private static LoginUserCommand Normalize(LoginUserCommand input)
        {
            return new LoginUserCommand
            {
                Username = UserValidationHelpers.NormalizeCollapsed(input.Username).ToLowerInvariant(),
                Password = input.Password
            };
        }
    }
}
