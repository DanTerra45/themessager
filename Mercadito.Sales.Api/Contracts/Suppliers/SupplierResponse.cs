namespace Mercadito.Sales.Api.Contracts.Suppliers;

public sealed record SupplierResponse(
    long Id,
    string Codigo,
    string Nombre,
    string Direccion,
    string Contacto,
    string Rubro,
    string Telefono);
