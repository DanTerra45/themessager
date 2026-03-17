using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.data.repository;
using Mercadito.src.shared.domain.factory;

namespace Mercadito.src.employees.domain.usecases
{
    public class EmployeeManagementUseCase(RepositoryCreator<EmployeeRepository> employeeRepositoryCreator) : IEmployeeManagementUseCase
    {
        private readonly RepositoryCreator<EmployeeRepository> _employeeRepositoryCreator = employeeRepositoryCreator;

        public async Task<(IReadOnlyList<Employee> Employees, int TotalPages)> GetPageAsync(
            int currentPage,
            int pageSize,
            string sortBy,
            string sortDirection,
            CancellationToken cancellationToken = default)
        {
            var employeeRepository = _employeeRepositoryCreator.Create();
            var totalCount = await employeeRepository.GetTotalEmployeesCountAsync(cancellationToken);
            var totalPages = CalculateTotalPages(totalCount, pageSize);
            var employees = await employeeRepository.GetEmployeesByPages(currentPage, pageSize, sortBy, sortDirection, cancellationToken);
            return (employees, totalPages);
        }

        public async Task<Employee?> GetForEditAsync(long employeeId, CancellationToken cancellationToken = default)
        {
            var employeeRepository = _employeeRepositoryCreator.Create();
            return await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        }

        public async Task<bool> DeleteAsync(long employeeId, CancellationToken cancellationToken = default)
        {
            var employeeRepository = _employeeRepositoryCreator.Create();
            var affectedRows = await employeeRepository.DeleteAsync(employeeId, cancellationToken);
            return affectedRows > 0;

        }

        private static int CalculateTotalPages(int totalItems, int pageSize)
        {
            if (totalItems == 0 || pageSize <= 0) return 1;
            return (totalItems + pageSize - 1) / pageSize;
        }
    }
}
