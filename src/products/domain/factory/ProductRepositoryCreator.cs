using Mercadito.src.products.data.repository;
using Mercadito.src.shared.domain.factory;

namespace Mercadito.src.products.domain.factory
{
    public class ProductRepositoryCreator(ProductRepository productRepository)
        : RepositoryCreator<ProductRepository>
    {
        private readonly ProductRepository _productRepository = productRepository;

        public override ProductRepository Create()
        {
            return _productRepository;
        }
    }
}
