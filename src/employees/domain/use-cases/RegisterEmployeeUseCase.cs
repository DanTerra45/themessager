using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.dto;
using Mercadito.src.employees.domain.repository;

namespace Mercadito.src.employees.domain.usecases
{
    public class RegisterEmployeeUseCase : IRegisterEmployeeUseCase
    {
        private readonly IEmployeeRepository _employeeRepository;

        public RegisterEmployeeUseCase(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<long> ExecuteAsync(CreateEmployeeDto employee, CancellationToken cancellationToken = default)
        {
            var employeeToCreate = new Employee
            {
                Ci = employee.Ci,
                Complemento = employee.Complemento,
                Nombres = employee.Nombres,
                PrimerApellido = employee.PrimerApellido,
                SegundoApellido = employee.SegundoApellido,
                Rol = employee.Rol,
                NumeroContacto = employee.NumeroContacto,
                IsActive = true
            };

            return await _employeeRepository.AddEmployeeAsync(employeeToCreate, cancellationToken);
        }
    }
}