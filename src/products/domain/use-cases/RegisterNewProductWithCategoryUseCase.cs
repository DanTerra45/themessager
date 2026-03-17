using Mercadito.src.products.domain.dto;
using Mercadito.src.products.domain.factory;
using Mercadito.src.products.domain.repository;

namespace Mercadito.src.products.domain.usecases
{
    public class RegisterNewProductWithCategoryUseCase(
        IProductFactory productFactory,
        IProductRepository productRepository) : IRegisterNewProductWithCategoryUseCase
    {

        private readonly IProductFactory _productFactory = productFactory;
        private readonly IProductRepository _productRepository = productRepository;

        public async Task ExecuteAsync(CreateProductDto newProduct, CancellationToken cancellationToken = default)
        {
            await _productRepository.AddProductWithCategoriesAsync(
                _productFactory.CreateForInsert(newProduct),
                newProduct.CategoryIds,
                cancellationToken);
        }
    }
}
