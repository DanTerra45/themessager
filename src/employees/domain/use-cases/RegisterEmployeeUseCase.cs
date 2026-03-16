using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.dto;
using Mercadito.src.employees.domain.repository;

namespace Mercadito.src.employees.domain.usecases
{
    public class RegisterEmployeeUseCase(IEmployeeRepository employeeRepository) : IRegisterEmployeeUseCase
    {
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;

        public async Task<long> ExecuteAsync(CreateEmployeeDto employee, CancellationToken cancellationToken = default)
        {
            var employeeToCreate = new Employee
            {
                Ci = employee.Ci,
                Complemento = NormalizeCiExtension(employee.Complemento),
                Nombres = NormalizeRequired(employee.Nombres),
                PrimerApellido = NormalizeRequired(employee.PrimerApellido),
                SegundoApellido = NormalizeOptional(employee.SegundoApellido),
                Rol = NormalizeRequired(employee.Rol),
                NumeroContacto = NormalizeRequired(employee.NumeroContacto)
            };

            return await _employeeRepository.AddEmployeeAsync(employeeToCreate, cancellationToken);
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

        private static string? NormalizeCiExtension(string? value)
        {
            var normalizedValue = NormalizeOptional(value);
            if (string.IsNullOrEmpty(normalizedValue))
            {
                return null;
            }

            return normalizedValue.ToUpperInvariant();
        }
    }
}
