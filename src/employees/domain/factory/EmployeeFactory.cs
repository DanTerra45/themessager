using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.dto;
using Shared.Domain;
using System;

namespace Mercadito.src.employees.domain.factory
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
                Rol = NormalizeRequired(dto.Rol),
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
                Rol = NormalizeRequired(dto.Rol),
                NumeroContacto = NormalizeContact(dto.NumeroContacto)
            };
        }

        // New: TryCreateForInsert validates and returns Result<Employee>
        public Result<Employee> TryCreateForInsert(CreateEmployeeDto dto)
        {
            if (dto == null) return Result<Employee>.Failure("Employee data is required.");

            // Example validations — keep small and domain-focused
            if (dto.Ci.HasValue && dto.Ci.Value == 0)
            {
                return Result<Employee>.Failure("CI cannot be 0.");
            }

            if (string.IsNullOrWhiteSpace(dto.Nombres))
            {
                return Result<Employee>.Failure("Nombres are required.");
            }

            if (string.IsNullOrWhiteSpace(dto.PrimerApellido))
            {
                return Result<Employee>.Failure("Primer apellido is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Rol))
            {
                return Result<Employee>.Failure("Rol is required.");
            }

            // All good — create entity
            var entity = CreateForInsert(dto);
            return Result<Employee>.Success(entity);
        }

        // New: TryCreateForUpdate validates and returns Result<Employee>
        public Result<Employee> TryCreateForUpdate(UpdateEmployeeDto dto)
        {
            if (dto == null) return Result<Employee>.Failure("Employee data is required.");
            if (dto.Id <= 0) return Result<Employee>.Failure("Employee Id is required for update.");
            if (dto.Ci.HasValue && dto.Ci.Value == 0)
            {
                return Result<Employee>.Failure("CI cannot be 0.");
            }

            if (string.IsNullOrWhiteSpace(dto.Nombres))
            {
                return Result<Employee>.Failure("Nombres are required.");
            }

            if (string.IsNullOrWhiteSpace(dto.PrimerApellido))
            {
                return Result<Employee>.Failure("Primer apellido is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Rol))
            {
                return Result<Employee>.Failure("Rol is required.");
            }

            var entity = CreateForUpdate(dto);
            return Result<Employee>.Success(entity);
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
