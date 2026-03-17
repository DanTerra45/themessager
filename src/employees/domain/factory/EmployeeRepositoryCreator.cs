using Mercadito.src.employees.data.repository;
using Mercadito.src.shared.domain.factory;

namespace Mercadito.src.employees.domain.factory
{
    public class EmployeeRepositoryCreator(EmployeeRepository employeeRepository)
        : RepositoryCreator<EmployeeRepository>
    {
        private readonly EmployeeRepository _employeeRepository = employeeRepository;

        public override EmployeeRepository Create()
        {
            return _employeeRepository;
        }
    }
}
