using Mercadito.src.products.domain.dto;
using Mercadito.src.products.domain.factory;
using Mercadito.src.products.domain.model;
using Mercadito.src.products.data.repository;
using Mercadito.src.shared.domain.factory;

namespace Mercadito.src.products.domain.usecases
{
    public class RegisterNewProductWithCategoryUseCase(
        IProductFactory productFactory,
        RepositoryCreator<ProductRepository> productRepositoryCreator) : IRegisterNewProductWithCategoryUseCase
    {

        private readonly IProductFactory _productFactory = productFactory;
        private readonly RepositoryCreator<ProductRepository> _productRepositoryCreator = productRepositoryCreator;

        public async Task ExecuteAsync(CreateProductDto newProduct, CancellationToken cancellationToken = default)
        {
            var productRepository = _productRepositoryCreator.Create();
            await productRepository.CreateAsync(
                new ProductWithCategoriesWriteModel
                {
                    Product = _productFactory.CreateForInsert(newProduct),
                    CategoryIds = newProduct.CategoryIds
                },
                cancellationToken);
        }
    }
}
