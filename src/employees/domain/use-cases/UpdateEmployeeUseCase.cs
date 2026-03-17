using Mercadito.src.employees.domain.dto;
using Mercadito.src.employees.data.repository;
using Mercadito.src.employees.domain.factory;
using Mercadito.src.shared.domain.factory;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.employees.domain.usecases
{
    public class UpdateEmployeeUseCase(
        RepositoryCreator<EmployeeRepository> employeeRepositoryCreator,
        IEmployeeFactory employeeFactory) : IUpdateEmployeeUseCase
    {
        private readonly RepositoryCreator<EmployeeRepository> _employeeRepositoryCreator = employeeRepositoryCreator;
        private readonly IEmployeeFactory _employeeFactory = employeeFactory;

        public async Task ExecuteAsync(UpdateEmployeeDto employee, CancellationToken cancellationToken = default)
        {
            var employeeRepository = _employeeRepositoryCreator.Create();
            var employeeToUpdate = _employeeFactory.CreateForUpdate(employee);

            var affectedRows = await employeeRepository.UpdateAsync(employeeToUpdate, cancellationToken);
            if (affectedRows == 0)
            {
                throw new ValidationException("Empleado no encontrado.");
            }
        }
    }
}
