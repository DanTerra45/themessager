using Mercadito.src.domain.shared.validation;
using Mercadito.src.application.users.models;
using Mercadito.src.domain.shared;
using System.Text.RegularExpressions;

namespace Mercadito.src.application.users.validation
{
    public sealed class CreateUserValidator : ICreateUserValidator
    {
        private const string EmailPattern = "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$";
        private static readonly string[] AllowedRoles = ["Admin", "Operador", "Auditor"];
        private readonly ValidationErrorBag _errors = new();
        private readonly StringRuleSet _stringRules = new();

        public CreateUserValidator()
        {
            _stringRules.Add("Email", StringValidationRules.Required("El correo es obligatorio."));
            _stringRules.Add("Email", StringValidationRules.MaxLength(100, "El correo no puede exceder 100 caracteres."));
            _stringRules.Add("Email", StringValidationRules.RegexMatch(EmailPattern, "El correo no tiene un formato válido.", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase));
            _stringRules.Add("Role", StringValidationRules.Required("El rol es obligatorio."));
            _stringRules.Add("Role", StringValidationRules.OneOf(AllowedRoles, "El rol debe ser Admin, Operador o Auditor."));
            _stringRules.Add("SetupUrlBase", StringValidationRules.Required("La URL de activación es obligatoria."));
            _stringRules.Add("SetupUrlBase", StringValidationRules.AbsoluteUri("La URL de activación es inválida."));
        }

        public Result<CreateUserDto> Validate(CreateUserDto input)
        {
            if (input == null)
            {
                return Result.Failure<CreateUserDto>("El usuario es obligatorio.");
            }

            _errors.Clear();
            var normalized = Normalize(input);

            ValidateEmail(normalized.Email);
            ValidateEmployee(normalized.EmployeeId);
            ValidateRole(normalized.Role);
            ValidateSetupUrlBase(normalized.SetupUrlBase);

            if (_errors.HasErrors)
            {
                return Result.Failure<CreateUserDto>(_errors.ToDictionary());
            }

            return Result.Success(normalized);
        }

        private void ValidateEmail(string email)
        {
            _stringRules.Validate("Email", email, _errors);
        }

        private void ValidateEmployee(long? employeeId)
        {
            if (!employeeId.HasValue)
            {
                return;
            }

            if (employeeId.Value <= 0)
            {
                _errors.Add("EmployeeId", "El empleado asociado es inválido.");
            }
        }

        private void ValidateRole(string role)
        {
            _stringRules.Validate("Role", role, _errors);
        }

        private void ValidateSetupUrlBase(string setupUrlBase)
        {
            _stringRules.Validate("SetupUrlBase", setupUrlBase, _errors);
        }

        private static CreateUserDto Normalize(CreateUserDto input)
        {
            return new CreateUserDto
            {
                Username = ValidationText.NormalizeLowerTrimmed(input.Username),
                Email = ValidationText.NormalizeTrimmed(input.Email),
                EmployeeId = input.EmployeeId,
                Role = ValidationText.NormalizeTrimmed(input.Role),
                SetupUrlBase = ValidationText.NormalizeTrimmed(input.SetupUrlBase)
            };
        }
    }
}
