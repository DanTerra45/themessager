namespace Mercadito.Frontend.Dtos.Employees;

public sealed record SaveEmployeeRequestDto(
    long? Ci,
    string? Complemento,
    string Nombres,
    string PrimerApellido,
    string? SegundoApellido,
    string Cargo,
    string NumeroContacto);
