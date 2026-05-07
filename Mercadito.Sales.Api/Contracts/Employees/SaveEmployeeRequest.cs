namespace Mercadito.Sales.Api.Contracts.Employees;

public sealed record SaveEmployeeRequest(
    long? Ci,
    string? Complemento,
    string Nombres,
    string PrimerApellido,
    string? SegundoApellido,
    string Cargo,
    string NumeroContacto);
