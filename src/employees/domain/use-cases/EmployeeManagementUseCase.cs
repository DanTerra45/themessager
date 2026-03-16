using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.repository;

namespace Mercadito.src.employees.domain.usecases
{
    public class EmployeeManagementUseCase(IEmployeeRepository employeeRepository) : IEmployeeManagementUseCase
    {
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;

        public async Task<(IReadOnlyList<Employee> Employees, int TotalPages)> GetPageAsync(int currentPage, int pageSize, CancellationToken cancellationToken = default)
        {
            var totalCount = await _employeeRepository.GetTotalEmployeesCountAsync(cancellationToken);
            var totalPages = CalculateTotalPages(totalCount, pageSize);
            var employees = await _employeeRepository.GetEmployeesByPages(currentPage, pageSize, cancellationToken);
            return (employees, totalPages);
        }

        public async Task<Employee?> GetForEditAsync(long employeeId, CancellationToken cancellationToken = default)
        {
            return await _employeeRepository.GetEmployeeByIdAsync(employeeId, cancellationToken);
        }

        public async Task<bool> DeleteAsync(long employeeId, CancellationToken cancellationToken = default)
        {
            var affectedRows = await _employeeRepository.DeleteEmployeeAsync(employeeId, cancellationToken);
            return affectedRows > 0;

        }

        private static int CalculateTotalPages(int totalItems, int pageSize)
        {
            if (totalItems == 0 || pageSize <= 0) return 1;
            return (totalItems + pageSize - 1) / pageSize;
        }
    }
}
