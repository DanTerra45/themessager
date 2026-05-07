using Mercadito.Frontend.Dtos.Common;
using Mercadito.Frontend.Dtos.Employees;

namespace Mercadito.Frontend.Adapters.Employees;

public interface IEmployeesApiAdapter
{
    Task<ApiResponseDto<EmployeePageDto>> GetEmployeesAsync(
        int pageSize,
        string sortBy,
        string sortDirection,
        long anchorEmployeeId,
        long cursorEmployeeId,
        bool isNextPage,
        string searchTerm,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<EmployeeDto>> GetEmployeeAsync(
        long employeeId,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> CreateEmployeeAsync(
        SaveEmployeeRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> UpdateEmployeeAsync(
        long employeeId,
        SaveEmployeeRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> DeleteEmployeeAsync(
        long employeeId,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);
}
