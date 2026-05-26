using ServicioEmpleados.Application.Employees.Models;
using ServicioEmpleados.Domain.Employees.Entities;

namespace ServicioEmpleados.Application.Employees.Ports.Output
{
    public interface IEmployeeRepository
    {
        Task<IReadOnlyList<EmployeeModel>> GetEmployeesByCursorAsync(int pageSize, string sortBy, string sortDirection, long cursorEmployeeId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EmployeeModel>> GetEmployeesFromAnchorAsync(int pageSize, string sortBy, string sortDirection, long anchorEmployeeId, string searchTerm, CancellationToken cancellationToken = default);
        Task<bool> HasEmployeesByCursorAsync(string sortBy, string sortDirection, long cursorEmployeeId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default);
        Task<EmployeeModel?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<long> CreateAsync(Employee employee, CancellationToken cancellationToken = default);
        Task<int> UpdateAsync(Employee employee, CancellationToken cancellationToken = default);
        Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}

