using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.dto;

namespace Mercadito.src.employees.domain.factory
{
    public class EmployeeFactory : IEmployeeFactory
    {
        public Employee CreateForInsert(CreateEmployeeDto dto)
        {
            return new Employee
            {
                Ci = dto.Ci,
                Complemento = NormalizeCiExtension(dto.Complemento),
                Nombres = NormalizePersonName(dto.Nombres),
                PrimerApellido = NormalizePersonName(dto.PrimerApellido),
                SegundoApellido = NormalizeOptionalPersonName(dto.SegundoApellido),
                Rol = NormalizeRequired(dto.Rol),
                NumeroContacto = NormalizeRequired(dto.NumeroContacto)
            };
        }

        public Employee CreateForUpdate(UpdateEmployeeDto dto)
        {
            return new Employee
            {
                Id = dto.Id,
                Ci = dto.Ci,
                Complemento = NormalizeCiExtension(dto.Complemento),
                Nombres = NormalizePersonName(dto.Nombres),
                PrimerApellido = NormalizePersonName(dto.PrimerApellido),
                SegundoApellido = NormalizeOptionalPersonName(dto.SegundoApellido),
                Rol = NormalizeRequired(dto.Rol),
                NumeroContacto = NormalizeRequired(dto.NumeroContacto)
            };
        }

        private static string NormalizeRequired(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Trim();
        }

        private static string? NormalizeOptional(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static string NormalizePersonName(string value)
        {
            return CollapseSpaces(NormalizeRequired(value));
        }

        private static string? NormalizeOptionalPersonName(string? value)
        {
            var normalizedValue = NormalizeOptional(value);
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return null;
            }

            return CollapseSpaces(normalizedValue);
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

        private static string CollapseSpaces(string value)
        {
            var segments = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', segments);
        }
    }
}
