using Mercadito.src.employees.data.entity;

namespace Mercadito.src.employees.domain.repository
{
    public interface IEmployeeRepository
    {
        Task<IReadOnlyList<Employee>> GetEmployeesByPages(
            int page,
            int pageSize,
            string sortBy,
            string sortDirection,
            CancellationToken cancellationToken = default);
        Task<int> GetTotalEmployeesCountAsync(CancellationToken cancellationToken = default);
        Task<Employee?> GetEmployeeByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<long> AddEmployeeAsync(Employee employee, CancellationToken cancellationToken = default);
        Task<int> UpdateEmployeeAsync(Employee employee, CancellationToken cancellationToken = default);
        Task<int> DeleteEmployeeAsync(long id, CancellationToken cancellationToken = default);
    }
}
