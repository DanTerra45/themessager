using Mercadito.src.domain.provedores.dto;
using Mercadito.src.domain.provedores.model;

namespace Mercadito.src.domain.provedores.repository
{
    public interface ISupplierFactory
    {
        Supplier CreateForInsert(CreateSupplierDto dto);
        Supplier CreateForUpdate(UpdateSupplierDto dto);
    }
}