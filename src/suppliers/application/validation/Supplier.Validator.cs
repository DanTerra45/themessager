using Mercadito.src.shared.domain.validator;
using Mercadito.src.suppliers.application.models;
using Shared.Domain;
using System.Text;
using System.Text.RegularExpressions;

namespace Mercadito.src.suppliers.application.validation
{
    public abstract class SupplierValidator
    {
        private const string SupplierCodePattern = "^PRV[0-9]{3}$";
        private const string BusinessNamePattern = "^[A-ZÁÉÍÓÚÑ0-9][A-Za-zÁÉÍÓÚáéíóúÑñ0-9 .,&'()/\\-]*$";
        private const string ContactNamePattern = "^[A-ZÁÉÍÓÚÑ][A-Za-zÁÉÍÓÚáéíóúÑñ]+(?:[ .'-][A-Za-zÁÉÍÓÚáéíóúÑñ]+)*$";
        private const string AddressPattern = "^[A-ZÁÉÍÓÚÑ0-9][A-Za-zÁÉÍÓÚáéíóúÑñ0-9 .,/#()\\-]*$";
        private const string RubroPattern = "^[A-ZÁÉÍÓÚÑ][a-záéíóúñ]+(?: [a-záéíóúñ]+)*$";

        protected readonly Dictionary<string, List<string>> errors = new();
        public readonly Dictionary<string, List<string>> hints = new();

        protected SupplierValidator()
        {
            ConfigureHints();
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

        protected CreateSupplierDto Normalize(CreateSupplierDto input)
        {
            return new CreateSupplierDto
            {
                Codigo = NormalizeWhitespace(input.Codigo).ToUpperInvariant(),
                Nombre = NormalizeWhitespace(input.Nombre),
                Direccion = NormalizeWhitespace(input.Direccion),
                Contacto = NormalizeWhitespace(input.Contacto),
                Rubro = NormalizeWhitespace(input.Rubro),
                Telefono = NormalizeOptional(input.Telefono)
            };
        }

        protected UpdateSupplierDto Normalize(UpdateSupplierDto input)
        {
            return new UpdateSupplierDto
            {
                Id = input.Id,
                Codigo = NormalizeWhitespace(input.Codigo).ToUpperInvariant(),
                Nombre = NormalizeWhitespace(input.Nombre),
                Direccion = NormalizeWhitespace(input.Direccion),
                Contacto = NormalizeWhitespace(input.Contacto),
                Rubro = NormalizeWhitespace(input.Rubro),
                Telefono = NormalizeOptional(input.Telefono)
            };
        }

        protected void ValidateCreateFields(CreateSupplierDto dto)
        {
            ValidateBusinessName(dto.Nombre);
            ValidateAddress(dto.Direccion);
            ValidateContact(dto.Contacto);
            ValidateRubro(dto.Rubro);
        }

        protected void ValidateUpdateFields(UpdateSupplierDto dto)
        {
            if (dto.Id <= 0)
            {
                AddError("Id", "El proveedor es inválido.");
            }

            ValidateCode(dto.Codigo);
            ValidateBusinessName(dto.Nombre);
            ValidateAddress(dto.Direccion);
            ValidateContact(dto.Contacto);
            ValidateRubro(dto.Rubro);
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

        protected void AddError(string field, string message)
        {
            if (!errors.ContainsKey(field))
            {
                errors[field] = [];
            }

            errors[field].Add(message);
        }

        private void AddHint(string field, string hint)
        {
            if (!hints.ContainsKey(field))
            {
                hints[field] = [];
            }

            hints[field].Add(hint);
        }

        private void ValidateCode(string code)
        {
            ValidateRequired("Codigo", code, "El código es obligatorio.");
            ValidateExactLength("Codigo", code, 6, "El código debe tener exactamente 6 caracteres.");
            ValidatePattern("Codigo", code, SupplierCodePattern, "El código debe tener formato PRV001.");
        }

        private void ValidateBusinessName(string name)
        {
            ValidateRequired("Nombre", name, "La razón social es obligatoria.");
            ValidateLengthBetween("Nombre", name, 3, 120, "La razón social debe tener entre 3 y 120 caracteres.");
            ValidateControlCharacters("Nombre", name, "La razón social contiene caracteres no permitidos.");
            ValidatePattern("Nombre", name, BusinessNamePattern, "La razón social solo admite letras, números y separadores comerciales válidos.");
        }

        private void ValidateAddress(string address)
        {
            ValidateRequired("Direccion", address, "La dirección es obligatoria.");
            ValidateLengthBetween("Direccion", address, 5, 150, "La dirección debe tener entre 5 y 150 caracteres.");
            ValidateControlCharacters("Direccion", address, "La dirección contiene caracteres no permitidos.");
            ValidatePattern("Direccion", address, AddressPattern, "La dirección solo admite letras, números y los signos permitidos.");
        }

        private void ValidateContact(string contact)
        {
            ValidateRequired("Contacto", contact, "El contacto es obligatorio.");
            ValidateLengthBetween("Contacto", contact, 3, 60, "El contacto debe tener entre 3 y 60 caracteres.");
            ValidateControlCharacters("Contacto", contact, "El contacto contiene caracteres no permitidos.");
            ValidatePattern("Contacto", contact, ContactNamePattern, "El contacto debe ser un nombre válido y no admite números ni símbolos extraños.");
        }

        private void ValidateRubro(string rubro)
        {
            ValidateRequired("Rubro", rubro, "El rubro es obligatorio.");
            ValidateLengthBetween("Rubro", rubro, 4, 50, "El rubro debe tener entre 4 y 50 caracteres.");
            ValidateControlCharacters("Rubro", rubro, "El rubro contiene caracteres no permitidos.");
            ValidatePattern("Rubro", rubro, RubroPattern, "El rubro debe empezar con mayúscula y continuar en minúscula.");
        }

        private void ValidateRequired(string field, string value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(field, message);
            }
        }

        private void ValidateLengthBetween(string field, string value, int minimum, int maximum, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (value.Length < minimum || value.Length > maximum)
            {
                AddError(field, message);
            }
        }

        private void ValidateExactLength(string field, string value, int length, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (value.Length != length)
            {
                AddError(field, message);
            }
        }

        private void ValidatePattern(string field, string value, string pattern, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant))
            {
                AddError(field, message);
            }
        }

        private void ValidateControlCharacters(string field, string value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            foreach (var character in value)
            {
                if (char.IsControl(character))
                {
                    AddError(field, message);
                    return;
                }
            }
        }

        private static string NormalizeWhitespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            var previousWasWhitespace = false;

            foreach (var character in value.Trim())
            {
                if (char.IsWhiteSpace(character))
                {
                    if (previousWasWhitespace)
                    {
                        continue;
                    }

                    builder.Append(' ');
                    previousWasWhitespace = true;
                    continue;
                }

                builder.Append(character);
                previousWasWhitespace = false;
            }

            return builder.ToString();
        }

        private static string NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : NormalizeWhitespace(value);
        }
    }

    public class CreateSupplierValidator : SupplierValidator, IValidator<CreateSupplierDto, SupplierDto>
    {
        public Result<SupplierDto> Validate(CreateSupplierDto input)
        {
            if (input == null)
            {
                return Result<SupplierDto>.Failure("El proveedor es obligatorio.");
            }

            ClearErrors();
            var normalized = Normalize(input);
            ValidateCreateFields(normalized);

            if (HasErrors())
            {
                return Result<SupplierDto>.Failure(GetErrors());
            }

            return Result<SupplierDto>.Success(new SupplierDto
            {
                Codigo = normalized.Codigo,
                Nombre = normalized.Nombre,
                Direccion = normalized.Direccion,
                Contacto = normalized.Contacto,
                Rubro = normalized.Rubro,
                Telefono = string.IsNullOrWhiteSpace(normalized.Telefono) ? string.Empty : normalized.Telefono
            });
        }
    }

    public class UpdateSupplierValidator : SupplierValidator, IValidator<UpdateSupplierDto, SupplierDto>
    {
        public Result<SupplierDto> Validate(UpdateSupplierDto input)
        {
            if (input == null)
            {
                return Result<SupplierDto>.Failure("El proveedor es obligatorio.");
            }

            ClearErrors();
            var normalized = Normalize(input);
            ValidateUpdateFields(normalized);

            if (HasErrors())
            {
                return Result<SupplierDto>.Failure(GetErrors());
            }

            return Result<SupplierDto>.Success(new SupplierDto
            {
                Id = normalized.Id,
                Codigo = normalized.Codigo,
                Nombre = normalized.Nombre,
                Direccion = normalized.Direccion,
                Contacto = normalized.Contacto,
                Rubro = normalized.Rubro,
                Telefono = string.IsNullOrWhiteSpace(normalized.Telefono) ? string.Empty : normalized.Telefono
            });
        }
    }
}
