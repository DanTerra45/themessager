using Mercadito.src.users.application.models;
using Shared.Domain;
using System.Text.RegularExpressions;

namespace Mercadito.src.users.application.validation
{
    public sealed class CreateUserValidator : ICreateUserValidator
    {
        private const string EmailPattern = "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$";
        private readonly Dictionary<string, List<string>> _errors = new();

        public Result<CreateUserDto> Validate(CreateUserDto input)
        {
            if (input == null)
            {
                return Result<CreateUserDto>.Failure("El usuario es obligatorio.");
            }

            _errors.Clear();
            var normalized = Normalize(input);

            ValidateEmail(normalized.Email);
            ValidateEmployee(normalized.EmployeeId);
            ValidateRole(normalized.Role);
            ValidateSetupUrlBase(normalized.SetupUrlBase);

            return _errors.Count > 0
                ? Result<CreateUserDto>.Failure(_errors)
                : Result<CreateUserDto>.Success(normalized);
        }

        private void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                UserValidationHelpers.AddError(_errors, "Email", "El correo es obligatorio.");
                return;
            }

            if (email.Length > 100)
            {
                UserValidationHelpers.AddError(_errors, "Email", "El correo no puede exceder 100 caracteres.");
            }

            if (!Regex.IsMatch(email, EmailPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            {
                UserValidationHelpers.AddError(_errors, "Email", "El correo no tiene un formato válido.");
            }
        }

        private void ValidateEmployee(long? employeeId)
        {
            if (!employeeId.HasValue)
            {
                return;
            }

            if (employeeId.Value <= 0)
            {
                UserValidationHelpers.AddError(_errors, "EmployeeId", "El empleado asociado es inválido.");
            }
        }

        private void ValidateRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                UserValidationHelpers.AddError(_errors, "Role", "El rol es obligatorio.");
                return;
            }

            if (!string.Equals(role, "Admin", StringComparison.Ordinal) &&
                !string.Equals(role, "Operador", StringComparison.Ordinal) &&
                !string.Equals(role, "Auditor", StringComparison.Ordinal))
            {
                UserValidationHelpers.AddError(_errors, "Role", "El rol debe ser Admin, Operador o Auditor.");
            }
        }

        private void ValidateSetupUrlBase(string setupUrlBase)
        {
            if (string.IsNullOrWhiteSpace(setupUrlBase))
            {
                UserValidationHelpers.AddError(_errors, "SetupUrlBase", "La URL de activación es obligatoria.");
                return;
            }

            if (!Uri.TryCreate(setupUrlBase, UriKind.Absolute, out _))
            {
                UserValidationHelpers.AddError(_errors, "SetupUrlBase", "La URL de activación es inválida.");
            }
        }

        private static CreateUserDto Normalize(CreateUserDto input)
        {
            return new CreateUserDto
            {
                Username = UserValidationHelpers.NormalizeCollapsed(input.Username).ToLowerInvariant(),
                Email = UserValidationHelpers.NormalizeCollapsed(input.Email),
                EmployeeId = input.EmployeeId,
                Role = UserValidationHelpers.NormalizeCollapsed(input.Role),
                SetupUrlBase = UserValidationHelpers.NormalizeCollapsed(input.SetupUrlBase)
            };
        }
    }
}
