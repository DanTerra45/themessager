using Mercadito.src.employees.domain.dto;
using Mercadito.src.employees.domain.factory;
using Mercadito.src.employees.domain.repository;

namespace Mercadito.src.employees.domain.usecases
{
    public class RegisterEmployeeUseCase(
        IEmployeeRepository employeeRepository,
        IEmployeeFactory employeeFactory) : IRegisterEmployeeUseCase
    {
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;
        private readonly IEmployeeFactory _employeeFactory = employeeFactory;

        public async Task<long> ExecuteAsync(CreateEmployeeDto employee, CancellationToken cancellationToken = default)
        {
            var employeeToCreate = _employeeFactory.CreateForInsert(employee);
            return await _employeeRepository.AddEmployeeAsync(employeeToCreate, cancellationToken);
        }
    }
}
