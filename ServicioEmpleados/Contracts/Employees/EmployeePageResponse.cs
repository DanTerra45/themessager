namespace ServicioEmpleados.Contracts.Employees;

public sealed record EmployeePageResponse(
    IReadOnlyList<EmployeeResponse> Employees,
    bool HasPreviousPage,
    bool HasNextPage);
