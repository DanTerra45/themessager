using Mercadito.src.shared.domain.validator;
using Mercadito.src.domain.provedores.dto;
using Mercadito.src.domain.provedores.model;
using System.Text.RegularExpressions;

namespace Mercadito.src.domain.provedores.validator
{
    public abstract class SupplierValidator
    {
        protected Dictionary<string, List<string>> errors = new();

        public readonly Dictionary<string, List<string>> hints;
        
        private readonly Dictionary<string, Tuple<Regex,string>> _patterns;
        private readonly Dictionary<string, Tuple<int,string>> _maxLengths;
        private readonly Dictionary<string, Tuple<Func<string, bool>,string>> _validationsForStrings;

        protected SupplierValidator()
        {
            _patterns = new Dictionary<string, Tuple<Regex,string>>();
            _maxLengths = new Dictionary<string, Tuple<int, string>>();
            _validationsForStrings = new Dictionary<string, Tuple<Func<string, bool>, string>>();
            hints = new Dictionary<string, List<string>>();
            InitCodeValidationParameters();
            InitNameValidationParameters();
            InitContactValidationParameters();
            InitDireccionValidationParameters();
            InitRubroValidationParameters();
        }

        private void InitCodeValidationParameters()
        {
            AddPattern("Codigo", new Regex("^[A-Z]{3}[0-9]{3}$"), "El codigo debe tener 3 letras mayusculas seguidas de 3 numeros");
            AddMaxLength("Codigo", 6,"El Codigo debe tener maximo 6 caracteres");
            AddValidationForString("Codigo", string.IsNullOrWhiteSpace, "El codigo no puede estar vacio");
            AddHint("Codigo", "Ejemplo: ABC123");
            AddHint("Codigo", "El codigo debe tener 3 letras mayusculas seguidas de 3 numeros");
        }
        private void InitNameValidationParameters()
        {
            AddPattern("Nombre", new Regex("^[a-zA-ZáéíóúÁÉÍÓÚñÑ\\s]+$"),"El nombre debe empezar con mayuscula y solo puede contener letras");
            AddMaxLength("Nombre", 30,"El nombre debe tener maximo 30 caracteres");
            AddValidationForString("Nombre", string.IsNullOrWhiteSpace,"El nombre no puede estar vacio");
            AddHint("Nombre", "Ejemplo: Distribuidora Norte");
            AddHint("Nombre", "El nombre debe empezar con mayuscula y solo puede contener letras");
        }
        private void InitContactValidationParameters()
        {
            AddPattern("Contacto", new Regex("^[a-zA-ZáéíóúÁÉÍÓÚñÑ\\s]+$"),"El contacto debe empezar con mayuscula y solo puede contener letras");
            AddMaxLength("Contacto", 24,"El contacto debe tener maximo 24 caracteres");
            AddValidationForString("Contacto", string.IsNullOrWhiteSpace,"El contacto no puede estar vacio");
            AddHint("Contacto", "Ejemplo: Carlos Paredes");
            AddHint("Contacto", "El contacto debe empezar con mayuscula y solo puede contener letras");
        }
        protected void InitDireccionValidationParameters()
        {
            AddPattern("Direccion", new Regex("^[a-zA-ZáéíóúÁÉÍÓÚñÑ\\s]+$"),"La direccion debe empezar con mayuscula y solo puede contener letras, guiones y numerales");
            AddMaxLength("Direccion", 100,"La direccion debe tener maximo 100 caracteres");
            AddValidationForString("Direccion", string.IsNullOrWhiteSpace,"La direccion no puede estar vacia");
            AddHint("Direccion", "Ejemplo: Av. Principal 123, Zona Centro");
            AddHint("Direccion", "La direccion debe empezar con mayuscula y solo puede contener letras, guiones y numerales");
        }
        protected void InitRubroValidationParameters()
        {
            AddMaxLength("Rubro", 30,"El rubro debe tener maximo 30 caracteres");
            AddValidationForString("Rubro", string.IsNullOrWhiteSpace,"El rubro no puede estar vacio");
            AddHint("Rubro", "Ejemplo: Alimentos secos");
            AddHint("Rubro", "El rubro debe empezar con mayuscula y solo puede contener letras");
        }
        protected void AddHint(string field, string hint)
        {
            if (!hints.ContainsKey(field))
            {
                hints[field] = new List<string>();
            }
            hints[field].Add(hint);
        }
        protected void AddPattern(string field, Regex pattern, string errorMessage)
        {
            _patterns[field] = Tuple.Create(pattern, errorMessage);
        }

        protected void AddMaxLength(string field, int maxLength, string errorMessage)
        {
            _maxLengths[field] = Tuple.Create(maxLength, errorMessage) ;
        }

        protected void AddValidationForString(string field, Func<string, bool> validation, string errorMessage)
        {
            _validationsForStrings[field] = Tuple.Create(validation, errorMessage);
        }

        protected void ValidateMaxLength(string field, string value)
        {
            if (_maxLengths.TryGetValue(field, out Tuple<int,string>? maxLength) && value.Length > maxLength.Item1)
            {
                AddError(field, maxLength.Item2);
            }
        }

        protected void ValidatePattern(string field, string value)
        {
            if (_patterns.TryGetValue(field, out Tuple<Regex,string>? pattern) && !pattern.Item1.IsMatch(value))
            {
                AddError(field, pattern.Item2);
            }
        }

        protected void ValidateString(string field, string value)
        {
            if (_validationsForStrings.TryGetValue(field, out Tuple<Func<string, bool>,string>? validation) && validation.Item1(value))
            {
                AddError(field, validation.Item2);
            }
        }

        protected void ValidateField(string field, string value)
        {
            ValidateString(field, value);
            ValidateMaxLength(field, value);
            ValidatePattern(field, value);
        }

        protected void ValidateName(string name) => ValidateField("Nombre", name);
        protected void ValidateCode(string code) => ValidateField("Codigo", code);
        protected void ValidateContact(string contact) => ValidateField("Contacto", contact);
        protected void ValidateAddress(string address) => ValidateField("Direccion", address);
        protected void ValidateRubro(string rubro) => ValidateField("Rubro", rubro);

        protected void AddError(string field, string message)
        {
            if (!errors.ContainsKey(field))
            {
                errors[field] = new List<string>();
            }
            errors[field].Add(message);
        }

        protected bool HasErrors() => errors.Count > 0;
        protected Dictionary<string, List<string>> GetErrors() => errors;
        protected void ClearErrors() => errors.Clear();
    }

    public class CreateSupplierValidator : SupplierValidator, IValidator<CreateSupplierDto, SupplierDto>
    {
        public CreateSupplierValidator()
        {
        }

        public Result<SupplierDto> Validate(CreateSupplierDto input)
        {
            ClearErrors();

            ValidateCode(input.Codigo);
            ValidateName(input.Nombre);
            ValidateAddress(input.Direccion);
            ValidateContact(input.Contacto);
            ValidateRubro(input.Rubro);

            if (HasErrors())
            {
                return Result<SupplierDto>.Failure(GetErrors());
            }

            return Result<SupplierDto>.Success(new SupplierDto
            {
                Codigo = input.Codigo,
                Nombre = input.Nombre,
                Direccion = input.Direccion,
                Contacto = input.Contacto,
                Rubro = input.Rubro
            });
        }
    }

    public class UpdateSupplierValidator : SupplierValidator, IValidator<UpdateSupplierDto, SupplierDto>
    {
        public UpdateSupplierValidator()
        {
            
        }

        public Result<SupplierDto> Validate(UpdateSupplierDto input)
        {
            ClearErrors();

            if (input.Id <= 0)
            {
                AddError("Id", "El ID debe ser válido");
            }

            ValidateCode(input.Codigo);
            ValidateName(input.Nombre);
            ValidateAddress(input.Direccion);
            ValidateContact(input.Contacto);
            ValidateRubro(input.Rubro);

            if (HasErrors())
            {
                return Result<SupplierDto>.Failure(GetErrors());
            }

            return Result<SupplierDto>.Success(new SupplierDto
            {
                Id = input.Id,
                Codigo = input.Codigo,
                Nombre = input.Nombre,
                Direccion = input.Direccion,
                Contacto = input.Contacto,
                Rubro = input.Rubro
            });
        }
    }
}