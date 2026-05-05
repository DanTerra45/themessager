using Mercadito.Users.Api.Domain.Shared.Validation;
using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Users.Validation
{
    public sealed class LoginUserValidator : ILoginUserValidator
    {
        private const string UsernamePattern = "^[a-z0-9._-]{4,40}$";
        private readonly ValidationErrorBag _errors = new();
        private readonly StringRuleSet _stringRules = new();

        public LoginUserValidator()
        {
            _stringRules.Add("Username", StringValidationRules.Required("El usuario es obligatorio."));
            _stringRules.Add("Username", StringValidationRules.LengthBetween(4, 40, "El usuario debe tener entre 4 y 40 caracteres."));
            _stringRules.Add("Username", StringValidationRules.RegexMatch(UsernamePattern, "El usuario solo admite minúsculas, números, punto, guion y guion bajo."));
            _stringRules.Add("Password", StringValidationRules.Required("La contraseña es obligatoria."));
            _stringRules.Add("Password", StringValidationRules.MaxLength(128, "La contraseña no puede exceder 128 caracteres."));
        }

        public Result<LoginUserCommand> Validate(LoginUserCommand input)
        {
            if (input == null)
            {
                return Result.Failure<LoginUserCommand>("La solicitud es obligatoria.");
            }

            _errors.Clear();
            var normalized = Normalize(input);

            _stringRules.Validate("Username", normalized.Username, _errors);
            _stringRules.Validate("Password", normalized.Password, _errors);

            if (_errors.HasErrors)
            {
                return Result.Failure<LoginUserCommand>(_errors.ToDictionary());
            }

            return Result.Success(normalized);
        }

        private static LoginUserCommand Normalize(LoginUserCommand input)
        {
            return new LoginUserCommand
            {
                Username = ValidationText.NormalizeLowerTrimmed(input.Username),
                Password = input.Password
            };
        }
    }
}
