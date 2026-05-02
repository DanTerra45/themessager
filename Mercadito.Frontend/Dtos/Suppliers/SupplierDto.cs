namespace Mercadito.Frontend.Dtos.Suppliers;

public sealed record SupplierDto(
    long Id,
    string Codigo,
    string Nombre,
    string Direccion,
    string Contacto,
    string Rubro,
    string Telefono);
