namespace ServicioFrontend.Dtos.Employees;

public sealed record EmployeePageDto(
    IReadOnlyList<EmployeeDto> Employees,
    bool HasPreviousPage,
    bool HasNextPage);
