using Mercadito.src.employees.domain.dto;
using Mercadito.src.employees.domain.factory;
using Mercadito.src.employees.domain.repository;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.employees.domain.usecases
{
    public class UpdateEmployeeUseCase(
        IEmployeeRepository employeeRepository,
        IEmployeeFactory employeeFactory) : IUpdateEmployeeUseCase
    {
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;
        private readonly IEmployeeFactory _employeeFactory = employeeFactory;

        public async Task ExecuteAsync(UpdateEmployeeDto employee, CancellationToken cancellationToken = default)
        {
            var employeeToUpdate = _employeeFactory.CreateForUpdate(employee);

            var affectedRows = await _employeeRepository.UpdateEmployeeAsync(employeeToUpdate, cancellationToken);
            if (affectedRows == 0)
            {
                throw new ValidationException("Empleado no encontrado.");
            }
        }
    }
}
