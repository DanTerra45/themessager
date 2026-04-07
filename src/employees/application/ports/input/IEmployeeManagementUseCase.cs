using Mercadito.src.employees.application.models;
using Mercadito.src.audit.domain.entities;
using Shared.Domain;

namespace Mercadito.src.employees.application.ports.input
{
    public interface IEmployeeManagementUseCase
    {
        Task<IReadOnlyList<EmployeeModel>> GetPageByCursorAsync(int pageSize, string sortBy, string sortDirection, long cursorEmployeeId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EmployeeModel>> GetPageFromAnchorAsync(int pageSize, string sortBy, string sortDirection, long anchorEmployeeId, string searchTerm, CancellationToken cancellationToken = default);
        Task<bool> HasEmployeesByCursorAsync(string sortBy, string sortDirection, long cursorEmployeeId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default);
        Task<UpdateEmployeeDto?> GetForEditAsync(long employeeId, CancellationToken cancellationToken = default);
        Task<Result> CreateAsync(CreateEmployeeDto employee, AuditActor actor, CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(UpdateEmployeeDto employee, AuditActor actor, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long employeeId, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
