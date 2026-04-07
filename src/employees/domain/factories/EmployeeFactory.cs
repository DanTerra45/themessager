using Mercadito.src.employees.domain.entities;
using Mercadito.src.employees.application.models;
using System;

namespace Mercadito.src.employees.domain.factories
{
    public class EmployeeFactory : IEmployeeFactory
    {
        public Employee CreateForInsert(CreateEmployeeDto dto)
        {
            return new Employee
            {
                Ci = dto.Ci.GetValueOrDefault(0),
                Complemento = NormalizeCiExtension(dto.Complemento),
                Nombres = NormalizePersonName(dto.Nombres),
                PrimerApellido = NormalizePersonName(dto.PrimerApellido),
                SegundoApellido = NormalizeOptionalPersonName(dto.SegundoApellido),
                Cargo = NormalizeRequired(dto.Cargo),
                NumeroContacto = NormalizeContact(dto.NumeroContacto)
            };
        }

        public Employee CreateForUpdate(UpdateEmployeeDto dto)
        {
            return new Employee
            {
                Id = dto.Id,
                Ci = dto.Ci.GetValueOrDefault(0),
                Complemento = NormalizeCiExtension(dto.Complemento),
                Nombres = NormalizePersonName(dto.Nombres),
                PrimerApellido = NormalizePersonName(dto.PrimerApellido),
                SegundoApellido = NormalizeOptionalPersonName(dto.SegundoApellido),
                Cargo = NormalizeRequired(dto.Cargo),
                NumeroContacto = NormalizeContact(dto.NumeroContacto)
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

        private static string NormalizeContact(string value)
        {
            var normalizedValue = NormalizeRequired(value);
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return string.Empty;
            }

            if (normalizedValue.StartsWith("+591", StringComparison.Ordinal))
            {
                return normalizedValue;
            }

            if (normalizedValue.Length == 8 && IsDigitsOnly(normalizedValue))
            {
                return $"+591{normalizedValue}";
            }

            return normalizedValue;
        }

        private static bool IsDigitsOnly(string value)
        {
            foreach (var character in value)
            {
                if (!char.IsDigit(character))
                {
                    return false;
                }
            }

            return value.Length > 0;
        }
    }
}
