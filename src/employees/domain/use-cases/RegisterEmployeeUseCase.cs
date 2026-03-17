using Mercadito.src.employees.domain.dto;
using Mercadito.src.employees.data.repository;
using Mercadito.src.employees.domain.factory;
using Mercadito.src.shared.domain.factory;

namespace Mercadito.src.employees.domain.usecases
{
    public class RegisterEmployeeUseCase(
        RepositoryCreator<EmployeeRepository> employeeRepositoryCreator,
        IEmployeeFactory employeeFactory) : IRegisterEmployeeUseCase
    {
        private readonly RepositoryCreator<EmployeeRepository> _employeeRepositoryCreator = employeeRepositoryCreator;
        private readonly IEmployeeFactory _employeeFactory = employeeFactory;

        public async Task<long> ExecuteAsync(CreateEmployeeDto employee, CancellationToken cancellationToken = default)
        {
            var employeeRepository = _employeeRepositoryCreator.Create();
            var employeeToCreate = _employeeFactory.CreateForInsert(employee);
            return await employeeRepository.CreateAsync(employeeToCreate, cancellationToken);
        }
    }
}
