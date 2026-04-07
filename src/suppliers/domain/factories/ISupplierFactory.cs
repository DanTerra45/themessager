using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.domain.entities;

namespace Mercadito.src.suppliers.domain.factories
{
    public interface ISupplierFactory
    {
        Supplier CreateForInsert(CreateSupplierDto dto);
        Supplier CreateForUpdate(UpdateSupplierDto dto);
    }
}
