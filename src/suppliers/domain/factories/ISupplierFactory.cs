using Mercadito.src.suppliers.domain.entities;

namespace Mercadito.src.suppliers.domain.factories
{
    public sealed record CreateSupplierValues(
        string Codigo,
        string Nombre,
        string Direccion,
        string Contacto,
        string Rubro,
        string Telefono);

    public sealed record UpdateSupplierValues(
        long Id,
        string Codigo,
        string Nombre,
        string Direccion,
        string Contacto,
        string Rubro,
        string Telefono);

    public interface ISupplierFactory
    {
        Supplier CreateForInsert(CreateSupplierValues input);
        Supplier CreateForUpdate(UpdateSupplierValues input);
    }
}
