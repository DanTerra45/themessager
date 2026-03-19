using Mercadito.src.employees.domain.dto;
using Mercadito.src.employees.domain.model;

namespace Mercadito.src.employees.domain.usecases
{
    public interface IEmployeeManagementUseCase
    {
        Task<(IReadOnlyList<EmployeeModel> Employees, int TotalPages)> GetPageAsync(
            int currentPage,
            int pageSize,
            string sortBy,
            string sortDirection,
            CancellationToken cancellationToken = default);
        Task<UpdateEmployeeDto?> GetForEditAsync(long employeeId, CancellationToken cancellationToken = default);
        Task CreateAsync(CreateEmployeeDto employee, CancellationToken cancellationToken = default);
        Task UpdateAsync(UpdateEmployeeDto employee, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long employeeId, CancellationToken cancellationToken = default);
    }
}
