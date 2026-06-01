namespace ServicioProveedores.Contracts.Suppliers;

public sealed record SaveSupplierRequest(
    string Codigo,
    string Nombre,
    string Direccion,
    string Contacto,
    string Rubro,
    string? Telefono);
