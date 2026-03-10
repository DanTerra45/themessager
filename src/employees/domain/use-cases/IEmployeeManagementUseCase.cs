using Mercadito.src.employees.data.entity;

namespace Mercadito.src.employees.domain.usecases
{
    public interface IEmployeeManagementUseCase
    {
        Task<(IReadOnlyList<Employee> Employees, int TotalPages)> GetPageAsync(int currentPage, int pageSize);
        Task<Employee?> GetForEditAsync(long employeeId);
        Task<bool> DeleteAsync(long employeeId);
    }
}
