using Mercadito.src.shared.domain.factory;
using Mercadito.src.domain.provedores.repository;

namespace Mercadito.src.domain.provedores.factory
{
     public class SupplierRepositoryCreator(SupplierRepository supplierRepository) : RepositoryCreator<SupplierRepository>
    {
        private readonly SupplierRepository _supplierRepository = supplierRepository;
        public override SupplierRepository Create()
        {
            return _supplierRepository;
        }
    }
}