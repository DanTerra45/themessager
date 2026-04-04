using Mercadito.src.employees.domain.dto;
using Mercadito.src.employees.domain.model;
using Shared.Domain;
using System.Threading;
using System.Threading.Tasks;

namespace Mercadito.src.employees.domain.usecases
{
    public interface IEmployeeManagementUseCase
    {
        Task<IReadOnlyList<EmployeeModel>> GetPageByCursorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorEmployeeId,
            bool isNextPage,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EmployeeModel>> GetPageFromAnchorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long anchorEmployeeId,
            CancellationToken cancellationToken = default);

        Task<bool> HasEmployeesByCursorAsync(
            string sortBy,
            string sortDirection,
            long cursorEmployeeId,
            bool isNextPage,
            CancellationToken cancellationToken = default);

        Task<UpdateEmployeeDto?> GetForEditAsync(long employeeId, CancellationToken cancellationToken = default);

        // Now returns a Result to represent validation outcome without exceptions
        Task<Result> CreateAsync(CreateEmployeeDto employee, CancellationToken cancellationToken = default);

        // Now returns a Result to represent validation outcome without exceptions
        Task<Result> UpdateAsync(UpdateEmployeeDto employee, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(long employeeId, CancellationToken cancellationToken = default);
    }
}
