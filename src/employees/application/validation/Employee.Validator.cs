using Mercadito.src.employees.application.models;
using Mercadito.src.employees.domain.entities;
using Mercadito.src.employees.domain.factories;
using Mercadito.src.shared.domain.validator;
using Shared.Domain;
using System.Text.RegularExpressions;

namespace Mercadito.src.employees.application.validation
{
    public interface ICreateEmployeeValidator : IValidator<CreateEmployeeDto, Employee>
    {
    }

    public interface IUpdateEmployeeValidator : IValidator<UpdateEmployeeDto, Employee>
    {
    }

    public abstract class EmployeeValidator
    {
        private const string HumanNamePattern = "^[A-Za-z\\u00C0-\\u024F]+(?:[ .'-][A-Za-z\\u00C0-\\u024F]+)*$";
        private const string ComplementPattern = "^[0-9][A-Za-z]$";
        private const string ContactPattern = "^(?:\\+591)?[0-9]{8}$";
        private const long MinimumCiValue = 1000000L;
        private const long MaximumCiValue = 99999999L;

        private readonly Dictionary<string, List<Func<string, string>>> _requiredStringRules = new();
        private readonly Dictionary<string, List<Func<string, string>>> _optionalStringRules = new();
        protected readonly Dictionary<string, List<string>> errors = new();

        protected EmployeeValidator()
        {
            ConfigureRequiredStringRules();
            ConfigureOptionalStringRules();
        }

        private void ConfigureRequiredStringRules()
        {
            ConfigureNameRules("Nombres", "Los nombres son requeridos", "Los nombres deben tener entre 2 y 40 caracteres", "Los nombres solo permiten letras y separadores válidos (espacio, punto, apóstrofe o guion)");
            ConfigureNameRules("PrimerApellido", "El primer apellido es requerido", "El primer apellido debe tener entre 2 y 40 caracteres", "El primer apellido solo permite letras y separadores válidos (espacio, punto, apóstrofe o guion)");

            AddRequiredStringRule("Cargo", value => Required(value, "El cargo es requerido"));
            AddRequiredStringRule("Cargo", value => RegexMatch(value, "^(Cajero|Inventario)$", "El cargo debe ser Cajero o Inventario"));

            AddRequiredStringRule("NumeroContacto", value => Required(value, "El número de contacto es requerido"));
            AddRequiredStringRule("NumeroContacto", value => LengthBetween(value, 8, 12, "El número de contacto debe tener 8 dígitos o incluir el prefijo +591"));
            AddRequiredStringRule("NumeroContacto", value => RegexMatch(value, ContactPattern, "El número de contacto debe tener formato válido (ejemplo: 71234567 o +59171234567)"));
        }

        private void ConfigureOptionalStringRules()
        {
            AddOptionalStringRule("Complemento", value => ExactLength(value, 2, "El complemento debe tener exactamente 2 caracteres"));
            AddOptionalStringRule("Complemento", value => RegexMatch(value, ComplementPattern, "El complemento debe tener formato número+letra (ejemplo: 1A)"));

            AddOptionalStringRule("SegundoApellido", value => MaxLength(value, 40, "Máximo 40 caracteres"));
            AddOptionalStringRule("SegundoApellido", value => RegexMatch(value, HumanNamePattern, "El segundo apellido solo permite letras y separadores válidos (espacio, punto, apóstrofe o guion)"));
        }

        private void ConfigureNameRules(string field, string requiredMessage, string lengthMessage, string formatMessage)
        {
            AddRequiredStringRule(field, value => Required(value, requiredMessage));
            AddRequiredStringRule(field, value => LengthBetween(value, 2, 40, lengthMessage));
            AddRequiredStringRule(field, value => RegexMatch(value, HumanNamePattern, formatMessage));
        }

        protected void ValidateCreateFields(CreateEmployeeDto dto)
        {
            ValidateCi(dto.Ci);
            ValidateRequiredField("Nombres", dto.Nombres);
            ValidateRequiredField("PrimerApellido", dto.PrimerApellido);
            ValidateOptionalField("SegundoApellido", dto.SegundoApellido);
            ValidateOptionalField("Complemento", dto.Complemento);
            ValidateRequiredField("Cargo", dto.Cargo);
            ValidateRequiredField("NumeroContacto", dto.NumeroContacto);
        }

        protected void ValidateUpdateFields(UpdateEmployeeDto dto)
        {
            if (dto.Id <= 0)
            {
                AddError("Id", "El empleado es inválido.");
            }

            ValidateCi(dto.Ci);
            ValidateRequiredField("Nombres", dto.Nombres);
            ValidateRequiredField("PrimerApellido", dto.PrimerApellido);
            ValidateOptionalField("SegundoApellido", dto.SegundoApellido);
            ValidateOptionalField("Complemento", dto.Complemento);
            ValidateRequiredField("Cargo", dto.Cargo);
            ValidateRequiredField("NumeroContacto", dto.NumeroContacto);
        }

        protected void ValidateCi(long? ci)
        {
            if (!ci.HasValue)
            {
                AddError("Ci", "El CI es obligatorio");
                return;
            }

            if (ci.Value < MinimumCiValue || ci.Value > MaximumCiValue)
            {
                AddError("Ci", "El CI debe tener entre 7 y 8 dígitos");
            }
        }

        protected void ValidateRequiredField(string field, string value)
        {
            if (!_requiredStringRules.TryGetValue(field, out var rules))
            {
                return;
            }

            foreach (var rule in rules)
            {
                var message = rule(value);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    AddError(field, message);
                }
            }
        }

        protected void ValidateOptionalField(string field, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!_optionalStringRules.TryGetValue(field, out var rules))
            {
                return;
            }

            foreach (var rule in rules)
            {
                var message = rule(value);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    AddError(field, message);
                }
            }
        }

        protected void AddRequiredStringRule(string field, Func<string, string> rule)
        {
            if (!_requiredStringRules.ContainsKey(field))
            {
                _requiredStringRules[field] = [];
            }

            _requiredStringRules[field].Add(rule);
        }

        protected void AddOptionalStringRule(string field, Func<string, string> rule)
        {
            if (!_optionalStringRules.ContainsKey(field))
            {
                _optionalStringRules[field] = [];
            }

            _optionalStringRules[field].Add(rule);
        }

        protected void AddError(string field, string message)
        {
            if (!errors.ContainsKey(field))
            {
                errors[field] = [];
            }

            errors[field].Add(message);
        }

        protected void ClearErrors()
        {
            errors.Clear();
        }

        protected bool HasErrors()
        {
            return errors.Count > 0;
        }

        protected Dictionary<string, List<string>> GetErrors()
        {
            return errors;
        }

        private static string Required(string value, string message)
        {
            return string.IsNullOrWhiteSpace(value) ? message : string.Empty;
        }

        private static string LengthBetween(string value, int minimum, int maximum, string message)
        {
            return string.IsNullOrWhiteSpace(value) || value.Length >= minimum && value.Length <= maximum ? string.Empty : message;
        }

        private static string ExactLength(string value, int length, string message)
        {
            return string.IsNullOrWhiteSpace(value) || value.Length == length ? string.Empty : message;
        }

        private static string MaxLength(string value, int maximum, string message)
        {
            return string.IsNullOrWhiteSpace(value) || value.Length <= maximum ? string.Empty : message;
        }

        private static string RegexMatch(string value, string pattern, string message)
        {
            return string.IsNullOrWhiteSpace(value) || Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant) ? string.Empty : message;
        }
    }

    public sealed class CreateEmployeeValidator : EmployeeValidator, ICreateEmployeeValidator
    {
        private readonly IEmployeeFactory _employeeFactory;

        public CreateEmployeeValidator(IEmployeeFactory employeeFactory)
        {
            _employeeFactory = employeeFactory;
        }

        public Result<Employee> Validate(CreateEmployeeDto input)
        {
            if (input == null)
            {
                return Result<Employee>.Failure("El empleado es obligatorio.");
            }

            ClearErrors();
            ValidateCreateFields(input);

            return HasErrors()
                ? Result<Employee>.Failure(GetErrors())
                : Result<Employee>.Success(_employeeFactory.CreateForInsert(input));
        }
    }

    public sealed class UpdateEmployeeValidator : EmployeeValidator, IUpdateEmployeeValidator
    {
        private readonly IEmployeeFactory _employeeFactory;

        public UpdateEmployeeValidator(IEmployeeFactory employeeFactory)
        {
            _employeeFactory = employeeFactory;
        }

        public Result<Employee> Validate(UpdateEmployeeDto input)
        {
            if (input == null)
            {
                return Result<Employee>.Failure("El empleado es obligatorio.");
            }

            ClearErrors();
            ValidateUpdateFields(input);

            return HasErrors()
                ? Result<Employee>.Failure(GetErrors())
                : Result<Employee>.Success(_employeeFactory.CreateForUpdate(input));
        }
    }
}
