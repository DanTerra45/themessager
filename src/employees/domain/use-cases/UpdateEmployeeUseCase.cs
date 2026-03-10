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
                Complemento = employee.Complemento,
                Nombres = employee.Nombres,
                PrimerApellido = employee.PrimerApellido,
                SegundoApellido = employee.SegundoApellido,
                Rol = employee.Rol,
                NumeroContacto = employee.NumeroContacto,
                IsActive = employee.IsActive
            };

            var affectedRows = await _employeeRepository.UpdateEmployeeAsync(employeeToUpdate, cancellationToken);
            if (affectedRows == 0)
            {
                throw new InvalidOperationException("Empleado no encontrado.");
            }
        }
    }
}
