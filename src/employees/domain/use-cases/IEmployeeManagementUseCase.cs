using Mercadito.src.employees.data.entity;

namespace Mercadito.src.employees.domain.usecases
{
    public interface IEmployeeManagementUseCase
    {
        Task<(IReadOnlyList<Employee> Employees, int TotalPages)> GetPageAsync(int currentPage, int pageSize, CancellationToken cancellationToken = default);
        Task<Employee?> GetForEditAsync(long employeeId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long employeeId, CancellationToken cancellationToken = default);
    }
}
