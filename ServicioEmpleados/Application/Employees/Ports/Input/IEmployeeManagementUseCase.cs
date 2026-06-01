using ServicioEmpleados.Application.Employees.Models;
using ServicioEmpleados.Domain.Audit.Entities;
using ServicioEmpleados.Domain.Shared;

namespace ServicioEmpleados.Application.Employees.Ports.Input
{
    public interface IEmployeeManagementUseCase
    {
        Task<Result<IReadOnlyList<EmployeeModel>>> GetPageByCursorAsync(int pageSize, string sortBy, string sortDirection, long cursorEmployeeId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default);
        Task<Result<IReadOnlyList<EmployeeModel>>> GetPageFromAnchorAsync(int pageSize, string sortBy, string sortDirection, long anchorEmployeeId, string searchTerm, CancellationToken cancellationToken = default);
        Task<Result<bool>> HasEmployeesByCursorAsync(string sortBy, string sortDirection, long cursorEmployeeId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default);
        Task<Result<UpdateEmployeeDto>> GetForEditAsync(long employeeId, CancellationToken cancellationToken = default);
        Task<Result> CreateAsync(CreateEmployeeDto employee, AuditActor actor, CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(UpdateEmployeeDto employee, AuditActor actor, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(long employeeId, AuditActor actor, CancellationToken cancellationToken = default);
    }
}

