namespace Mercadito.Frontend.Dtos.Suppliers;

public sealed record SaveSupplierRequestDto(
    string Codigo,
    string Nombre,
    string Direccion,
    string Contacto,
    string Rubro,
    string? Telefono);
