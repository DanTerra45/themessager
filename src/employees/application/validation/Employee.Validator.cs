using Mercadito.src.employees.application.models;
using Mercadito.src.employees.domain.entities;
using Mercadito.src.employees.domain.factories;
using Mercadito.src.shared.domain.validation;
using Mercadito.src.shared.domain;

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
        private const string ContactPattern = "^[0-9]{8}$";
        private const long MinimumCiValue = 1000000L;
        private const long MaximumCiValue = 99999999L;

        private readonly StringRuleSet _requiredStringRules = new();
        private readonly StringRuleSet _optionalStringRules = new();
        private readonly ValidationErrorBag _errors = new();

        protected EmployeeValidator()
        {
            ConfigureRequiredStringRules();
            ConfigureOptionalStringRules();
        }

        private void ConfigureRequiredStringRules()
        {
            ConfigureNameRules("Nombres", "Los nombres son requeridos", "Los nombres deben tener entre 2 y 40 caracteres", "Los nombres solo permiten letras y separadores válidos (espacio, punto, apóstrofe o guion)");
            ConfigureNameRules("PrimerApellido", "El primer apellido es requerido", "El primer apellido debe tener entre 2 y 40 caracteres", "El primer apellido solo permite letras y separadores válidos (espacio, punto, apóstrofe o guion)");

            AddRequiredStringRule("Cargo", StringValidationRules.Required("El cargo es requerido"));
            AddRequiredStringRule("Cargo", StringValidationRules.RegexMatch("^(Cajero|Inventario)$", "El cargo debe ser Cajero o Inventario"));

            AddRequiredStringRule("NumeroContacto", StringValidationRules.Required("El número de contacto es requerido"));
            AddRequiredStringRule("NumeroContacto", StringValidationRules.ExactLength(8, "El número de contacto debe tener exactamente 8 dígitos"));
            AddRequiredStringRule("NumeroContacto", StringValidationRules.RegexMatch(ContactPattern, "El número de contacto debe tener formato válido (ejemplo: 71234567)"));
        }

        private void ConfigureOptionalStringRules()
        {
            AddOptionalStringRule("Complemento", StringValidationRules.ExactLength(2, "El complemento debe tener exactamente 2 caracteres"));
            AddOptionalStringRule("Complemento", StringValidationRules.RegexMatch(ComplementPattern, "El complemento debe tener formato número+letra (ejemplo: 1A)"));

            AddOptionalStringRule("SegundoApellido", StringValidationRules.MaxLength(40, "Máximo 40 caracteres"));
            AddOptionalStringRule("SegundoApellido", StringValidationRules.RegexMatch(HumanNamePattern, "El segundo apellido solo permite letras y separadores válidos (espacio, punto, apóstrofe o guion)"));
        }

        private void ConfigureNameRules(string field, string requiredMessage, string lengthMessage, string formatMessage)
        {
            AddRequiredStringRule(field, StringValidationRules.Required(requiredMessage));
            AddRequiredStringRule(field, StringValidationRules.LengthBetween(2, 40, lengthMessage));
            AddRequiredStringRule(field, StringValidationRules.RegexMatch(HumanNamePattern, formatMessage));
        }

        protected void ValidateCreateFields(CreateEmployeeDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

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
            ArgumentNullException.ThrowIfNull(dto);

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
            _requiredStringRules.Validate(field, value, _errors);
        }

        protected void ValidateOptionalField(string field, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            _optionalStringRules.Validate(field, value, _errors);
        }

        protected void AddRequiredStringRule(string field, Func<string, string> rule)
        {
            _requiredStringRules.Add(field, rule);
        }

        protected void AddOptionalStringRule(string field, Func<string, string> rule)
        {
            _optionalStringRules.Add(field, rule);
        }

        protected void AddError(string field, string message)
        {
            _errors.Add(field, message);
        }

        protected void ClearErrors()
        {
            _errors.Clear();
        }

        protected bool HasErrors()
        {
            return _errors.HasErrors;
        }

        protected Dictionary<string, List<string>> GetErrors()
        {
            return _errors.ToDictionary();
        }
    }

    public sealed class CreateEmployeeValidator(IEmployeeFactory employeeFactory) : EmployeeValidator, ICreateEmployeeValidator
    {
        public Result<Employee> Validate(CreateEmployeeDto input)
        {
            if (input == null)
            {
                return Result.Failure<Employee>("El empleado es obligatorio.");
            }

            var normalizedInput = EmployeeValidationNormalization.NormalizeCreateInput(input);
            ClearErrors();
            ValidateCreateFields(normalizedInput);

            if (HasErrors())
            {
                return Result.Failure<Employee>(GetErrors());
            }

            return Result.Success(employeeFactory.CreateForInsert(EmployeeValidationNormalization.ToCreateValues(normalizedInput)));
        }
    }

    public sealed class UpdateEmployeeValidator(IEmployeeFactory employeeFactory) : EmployeeValidator, IUpdateEmployeeValidator
    {
        public Result<Employee> Validate(UpdateEmployeeDto input)
        {
            if (input == null)
            {
                return Result.Failure<Employee>("El empleado es obligatorio.");
            }

            var normalizedInput = EmployeeValidationNormalization.NormalizeUpdateInput(input);
            ClearErrors();
            ValidateUpdateFields(normalizedInput);

            if (HasErrors())
            {
                return Result.Failure<Employee>(GetErrors());
            }

            return Result.Success(employeeFactory.CreateForUpdate(EmployeeValidationNormalization.ToUpdateValues(normalizedInput)));
        }
    }

    internal static class EmployeeValidationNormalization
    {
        internal static CreateEmployeeDto NormalizeCreateInput(CreateEmployeeDto input)
        {
            return new CreateEmployeeDto
            {
                Ci = input.Ci,
                Complemento = NormalizeComplemento(input.Complemento),
                Nombres = ValidationText.NormalizeCollapsed(input.Nombres),
                PrimerApellido = ValidationText.NormalizeCollapsed(input.PrimerApellido),
                SegundoApellido = NormalizeOptionalName(input.SegundoApellido),
                Cargo = ValidationText.NormalizeTrimmed(input.Cargo),
                NumeroContacto = ValidationText.NormalizeTrimmed(input.NumeroContacto)
            };
        }

        internal static CreateEmployeeValues ToCreateValues(CreateEmployeeDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new CreateEmployeeValues(
                input.Ci,
                input.Complemento,
                input.Nombres,
                input.PrimerApellido,
                input.SegundoApellido,
                input.Cargo,
                input.NumeroContacto);
        }

        internal static UpdateEmployeeDto NormalizeUpdateInput(UpdateEmployeeDto input)
        {
            return new UpdateEmployeeDto
            {
                Id = input.Id,
                Ci = input.Ci,
                Complemento = NormalizeComplemento(input.Complemento),
                Nombres = ValidationText.NormalizeCollapsed(input.Nombres),
                PrimerApellido = ValidationText.NormalizeCollapsed(input.PrimerApellido),
                SegundoApellido = NormalizeOptionalName(input.SegundoApellido),
                Cargo = ValidationText.NormalizeTrimmed(input.Cargo),
                NumeroContacto = ValidationText.NormalizeTrimmed(input.NumeroContacto)
            };
        }

        internal static UpdateEmployeeValues ToUpdateValues(UpdateEmployeeDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new UpdateEmployeeValues(
                input.Id,
                input.Ci,
                input.Complemento,
                input.Nombres,
                input.PrimerApellido,
                input.SegundoApellido,
                input.Cargo,
                input.NumeroContacto);
        }

        private static string? NormalizeComplemento(string? value)
        {
            var normalizedValue = ValidationText.NormalizeTrimmed(value);
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return null;
            }

            return normalizedValue.ToUpperInvariant();
        }

        private static string? NormalizeOptionalName(string? value)
        {
            var normalizedValue = ValidationText.NormalizeCollapsed(value);
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return null;
            }

            return normalizedValue;
        }
    }
}
