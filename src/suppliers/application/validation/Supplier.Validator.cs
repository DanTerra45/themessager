using Mercadito.src.shared.domain.validation;
using Mercadito.src.suppliers.application.models;
using Mercadito.src.shared.domain;

namespace Mercadito.src.suppliers.application.validation
{
    public abstract class SupplierValidator : ISupplierFormHintsProvider
    {
        private const string SupplierCodePattern = "^PRV[0-9]{3}$";
        private const string BusinessNamePattern = "^[A-ZÁÉÍÓÚÑ0-9][A-Za-zÁÉÍÓÚáéíóúÑñ0-9 .,&'()/\\-]*$";
        private const string ContactNamePattern = "^[A-ZÁÉÍÓÚÑ][A-Za-zÁÉÍÓÚáéíóúÑñ]+(?:[ .'-][A-Za-zÁÉÍÓÚáéíóúÑñ]+)*$";
        private const string AddressPattern = "^[A-ZÁÉÍÓÚÑ0-9][A-Za-zÁÉÍÓÚáéíóúÑñ0-9 .,/#()\\-]*$";
        private const string RubroPattern = "^[A-ZÁÉÍÓÚÑ][a-záéíóúñ]+(?: [a-záéíóúñ]+)*$";

        private readonly StringRuleSet _stringRules = new();
        private readonly ValidationErrorBag _errors = new();
        private readonly Dictionary<string, List<string>> _hints = [];

        protected SupplierValidator()
        {
            ConfigureHints();
            ConfigureStringRules();
        }

        private void ConfigureHints()
        {
            AddHint("Codigo", "Se genera automáticamente al guardar el proveedor.");
            AddHint("Codigo", "Formato interno: PRV001.");

            AddHint("Nombre", "Ejemplo: Distribuidora Centro S.R.L.");
            AddHint("Nombre", "Debe tener entre 3 y 120 caracteres.");
            AddHint("Nombre", "Permite letras, números y separadores comerciales comunes.");

            AddHint("Direccion", "Ejemplo: Av. Principal 123, Zona Centro");
            AddHint("Direccion", "Debe tener entre 5 y 150 caracteres.");
            AddHint("Direccion", "Permite letras, números, espacios y solo estos signos: punto, coma, guion, numeral, diagonal y paréntesis.");

            AddHint("Contacto", "Ejemplo: Carlos Paredes");
            AddHint("Contacto", "Debe tener entre 3 y 60 caracteres.");
            AddHint("Contacto", "Solo admite nombres con letras y separadores válidos.");

            AddHint("Rubro", "Ejemplo: Alimentos secos");
            AddHint("Rubro", "Debe tener entre 4 y 50 caracteres.");
            AddHint("Rubro", "Solo la primera letra va en mayúscula; el resto debe ir en minúscula.");
        }

        private void ConfigureStringRules()
        {
            ConfigureCodeRules();
            ConfigureBusinessNameRules();
            ConfigureAddressRules();
            ConfigureContactRules();
            ConfigureRubroRules();
        }

        private void ConfigureCodeRules()
        {
            AddStringRule("Codigo", StringValidationRules.Required("El código es obligatorio."));
            AddStringRule("Codigo", StringValidationRules.ExactLength(6, "El código debe tener exactamente 6 caracteres."));
            AddStringRule("Codigo", StringValidationRules.RegexMatch(SupplierCodePattern, "El código debe tener formato PRV001."));
        }

        private void ConfigureBusinessNameRules()
        {
            AddStringRule("Nombre", StringValidationRules.Required("La razón social es obligatoria."));
            AddStringRule("Nombre", StringValidationRules.LengthBetween(3, 120, "La razón social debe tener entre 3 y 120 caracteres."));
            AddStringRule("Nombre", StringValidationRules.ControlCharacters("La razón social contiene caracteres no permitidos."));
            AddStringRule("Nombre", StringValidationRules.RegexMatch(BusinessNamePattern, "La razón social solo admite letras, números y separadores comerciales válidos."));
        }

        private void ConfigureAddressRules()
        {
            AddStringRule("Direccion", StringValidationRules.Required("La dirección es obligatoria."));
            AddStringRule("Direccion", StringValidationRules.LengthBetween(5, 150, "La dirección debe tener entre 5 y 150 caracteres."));
            AddStringRule("Direccion", StringValidationRules.ControlCharacters("La dirección contiene caracteres no permitidos."));
            AddStringRule("Direccion", StringValidationRules.RegexMatch(AddressPattern, "La dirección solo admite letras, números y los signos permitidos."));
        }

        private void ConfigureContactRules()
        {
            AddStringRule("Contacto", StringValidationRules.Required("El contacto es obligatorio."));
            AddStringRule("Contacto", StringValidationRules.LengthBetween(3, 60, "El contacto debe tener entre 3 y 60 caracteres."));
            AddStringRule("Contacto", StringValidationRules.ControlCharacters("El contacto contiene caracteres no permitidos."));
            AddStringRule("Contacto", StringValidationRules.RegexMatch(ContactNamePattern, "El contacto debe ser un nombre válido y no admite números ni símbolos extraños."));
        }

        private void ConfigureRubroRules()
        {
            AddStringRule("Rubro", StringValidationRules.Required("El rubro es obligatorio."));
            AddStringRule("Rubro", StringValidationRules.LengthBetween(4, 50, "El rubro debe tener entre 4 y 50 caracteres."));
            AddStringRule("Rubro", StringValidationRules.ControlCharacters("El rubro contiene caracteres no permitidos."));
            AddStringRule("Rubro", StringValidationRules.RegexMatch(RubroPattern, "El rubro debe empezar con mayúscula y continuar en minúscula."));
        }

        protected CreateSupplierDto Normalize(CreateSupplierDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new CreateSupplierDto
            {
                Codigo = ValidationText.NormalizeUpperTrimmed(input.Codigo),
                Nombre = ValidationText.NormalizeCollapsed(input.Nombre),
                Direccion = ValidationText.NormalizeCollapsed(input.Direccion),
                Contacto = ValidationText.NormalizeCollapsed(input.Contacto),
                Rubro = ValidationText.NormalizeCollapsed(input.Rubro),
                Telefono = ValidationText.NormalizeTrimmed(input.Telefono)
            };
        }

        protected UpdateSupplierDto Normalize(UpdateSupplierDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new UpdateSupplierDto
            {
                Id = input.Id,
                Codigo = ValidationText.NormalizeUpperTrimmed(input.Codigo),
                Nombre = ValidationText.NormalizeCollapsed(input.Nombre),
                Direccion = ValidationText.NormalizeCollapsed(input.Direccion),
                Contacto = ValidationText.NormalizeCollapsed(input.Contacto),
                Rubro = ValidationText.NormalizeCollapsed(input.Rubro),
                Telefono = ValidationText.NormalizeTrimmed(input.Telefono)
            };
        }

        protected void ValidateCreateFields(CreateSupplierDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            ValidateField("Nombre", dto.Nombre);
            ValidateField("Direccion", dto.Direccion);
            ValidateField("Contacto", dto.Contacto);
            ValidateField("Rubro", dto.Rubro);
        }

        protected void ValidateUpdateFields(UpdateSupplierDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (dto.Id <= 0)
            {
                AddError("Id", "El proveedor es inválido.");
            }

            ValidateField("Codigo", dto.Codigo);
            ValidateField("Nombre", dto.Nombre);
            ValidateField("Direccion", dto.Direccion);
            ValidateField("Contacto", dto.Contacto);
            ValidateField("Rubro", dto.Rubro);
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

        protected void AddError(string field, string message)
        {
            _errors.Add(field, message);
        }

        protected void ValidateField(string field, string value)
        {
            _stringRules.Validate(field, value, _errors);
        }

        protected void AddStringRule(string field, Func<string, string> rule)
        {
            _stringRules.Add(field, rule);
        }

        protected static string ResolveTelefono(string? telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
            {
                return string.Empty;
            }

            return telefono;
        }

        private void AddHint(string field, string hint)
        {
            if (!_hints.TryGetValue(field, out var fieldHints))
            {
                fieldHints = [];
                _hints[field] = fieldHints;
            }

            fieldHints.Add(hint);
        }

        public IReadOnlyDictionary<string, IReadOnlyList<string>> GetHints()
        {
            var copy = new Dictionary<string, IReadOnlyList<string>>(_hints.Count);
            foreach (var hint in _hints)
            {
                copy[hint.Key] = [.. hint.Value];
            }

            return copy;
        }
    }

    public class CreateSupplierValidator : SupplierValidator, IValidator<CreateSupplierDto, SupplierDto>
    {
        public Result<SupplierDto> Validate(CreateSupplierDto input)
        {
            if (input == null)
            {
                return Result.Failure<SupplierDto>("El proveedor es obligatorio.");
            }

            ClearErrors();
            var normalized = Normalize(input);
            ValidateCreateFields(normalized);

            if (HasErrors())
            {
                return Result.Failure<SupplierDto>(GetErrors());
            }

            return Result.Success(new SupplierDto
            {
                Codigo = normalized.Codigo,
                Nombre = normalized.Nombre,
                Direccion = normalized.Direccion,
                Contacto = normalized.Contacto,
                Rubro = normalized.Rubro,
                Telefono = ResolveTelefono(normalized.Telefono)
            });
        }
    }

    public class UpdateSupplierValidator : SupplierValidator, IValidator<UpdateSupplierDto, SupplierDto>
    {
        public Result<SupplierDto> Validate(UpdateSupplierDto input)
        {
            if (input == null)
            {
                return Result.Failure<SupplierDto>("El proveedor es obligatorio.");
            }

            ClearErrors();
            var normalized = Normalize(input);
            ValidateUpdateFields(normalized);

            if (HasErrors())
            {
                return Result.Failure<SupplierDto>(GetErrors());
            }

            return Result.Success(new SupplierDto
            {
                Id = normalized.Id,
                Codigo = normalized.Codigo,
                Nombre = normalized.Nombre,
                Direccion = normalized.Direccion,
                Contacto = normalized.Contacto,
                Rubro = normalized.Rubro,
                Telefono = ResolveTelefono(normalized.Telefono)
            });
        }
    }
}
