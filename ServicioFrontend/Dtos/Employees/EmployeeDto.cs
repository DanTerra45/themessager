namespace ServicioFrontend.Dtos.Employees;

public sealed record EmployeeDto(
    long Id,
    long Ci,
    string? Complemento,
    string Nombres,
    string PrimerApellido,
    string? SegundoApellido,
    string Cargo,
    string NumeroContacto);
