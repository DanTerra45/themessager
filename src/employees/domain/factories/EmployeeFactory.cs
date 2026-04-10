using Mercadito.src.employees.domain.entities;
using Mercadito.src.shared.domain.validation;

namespace Mercadito.src.employees.domain.factories
{
    public class EmployeeFactory : IEmployeeFactory
    {
        public Employee CreateForInsert(CreateEmployeeValues input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new Employee
            {
                Ci = input.Ci.GetValueOrDefault(0),
                Complemento = NormalizeCiExtension(input.Complemento),
                Nombres = ValidationText.NormalizeCollapsed(input.Nombres),
                PrimerApellido = ValidationText.NormalizeCollapsed(input.PrimerApellido),
                SegundoApellido = NormalizeOptionalPersonName(input.SegundoApellido),
                Cargo = ValidationText.NormalizeTrimmed(input.Cargo),
                NumeroContacto = NormalizeContact(input.NumeroContacto)
            };
        }

        public Employee CreateForUpdate(UpdateEmployeeValues input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new Employee
            {
                Id = input.Id,
                Ci = input.Ci.GetValueOrDefault(0),
                Complemento = NormalizeCiExtension(input.Complemento),
                Nombres = ValidationText.NormalizeCollapsed(input.Nombres),
                PrimerApellido = ValidationText.NormalizeCollapsed(input.PrimerApellido),
                SegundoApellido = NormalizeOptionalPersonName(input.SegundoApellido),
                Cargo = ValidationText.NormalizeTrimmed(input.Cargo),
                NumeroContacto = NormalizeContact(input.NumeroContacto)
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            var normalizedValue = ValidationText.NormalizeTrimmed(value);
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return null;
            }

            return normalizedValue;
        }

        private static string? NormalizeOptionalPersonName(string? value)
        {
            var normalizedValue = NormalizeOptional(value);
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return null;
            }

            return ValidationText.NormalizeCollapsed(normalizedValue);
        }

        private static string? NormalizeCiExtension(string? value)
        {
            var normalizedValue = NormalizeOptional(value);
            if (string.IsNullOrEmpty(normalizedValue))
            {
                return null;
            }

            return normalizedValue.ToUpperInvariant();
        }

        private static string NormalizeContact(string value)
        {
            var normalizedValue = ValidationText.NormalizeTrimmed(value);
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return string.Empty;
            }

            return normalizedValue;
        }
    }
}
