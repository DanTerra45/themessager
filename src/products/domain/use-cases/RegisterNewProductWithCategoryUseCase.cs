using Mercadito.src.products.domain.dto;
using Mercadito.src.products.data.entity;
using Mercadito.src.products.domain.repository;

namespace Mercadito.src.products.domain.usecases
{
    public class RegisterNewProductWithCategoryUseCase(
        IProductRepository productRepository) : IRegisterNewProductWithCategoryUseCase
    {

        private readonly IProductRepository _productRepository = productRepository;

        public async Task ExecuteAsync(CreateProductDto newProduct, CancellationToken cancellationToken = default)
        {
            await _productRepository.AddProductWithCategoriesAsync(
                ToProductEntity(newProduct),
                newProduct.CategoryIds,
                cancellationToken);
        }

        private static Product ToProductEntity(CreateProductDto dto)
        {
            return new Product
            {
                Name = NormalizeRequired(dto.Name),
                Description = NormalizeRequired(dto.Description),
                Stock = dto.Stock,
                Batch = NormalizeRequired(dto.Batch),
                ExpirationDate = dto.ExpirationDate,
                Price = dto.Price
            };
        }

        private static string NormalizeRequired(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Trim();
        }
    }
}
