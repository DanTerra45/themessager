using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.dto;
using Mercadito.src.employees.domain.repository;

namespace Mercadito.src.employees.domain.usecases
{
    public class UpdateEmployeeUseCase : IUpdateEmployeeUseCase
    {
        private readonly IEmployeeRepository _employeeRepository;

        public UpdateEmployeeUseCase(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task ExecuteAsync(UpdateEmployeeDto employee, CancellationToken cancellationToken = default)
        {
            var employeeToUpdate = new Employee
            {
                Id = employee.Id,
                Ci = employee.Ci,
                Complemento = NormalizeCiExtension(employee.Complemento),
                Nombres = NormalizeRequired(employee.Nombres),
                PrimerApellido = NormalizeRequired(employee.PrimerApellido),
                SegundoApellido = NormalizeOptional(employee.SegundoApellido),
                Rol = NormalizeRequired(employee.Rol),
                NumeroContacto = NormalizeRequired(employee.NumeroContacto),
                IsActive = employee.IsActive
            };

            var affectedRows = await _employeeRepository.UpdateEmployeeAsync(employeeToUpdate, cancellationToken);
            if (affectedRows == 0)
            {
                throw new InvalidOperationException("Empleado no encontrado.");
            }
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
