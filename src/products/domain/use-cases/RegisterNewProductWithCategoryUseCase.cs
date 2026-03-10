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
            var categoryIds = GetDistinctCategoryIds(newProduct.CategoryIds);
            await _productRepository.AddProductWithCategoriesAsync(
                ToProductEntity(newProduct),
            categoryIds,
            cancellationToken);
        }

        private static List<long> GetDistinctCategoryIds(IReadOnlyList<long> categoryIds)
        {
            var distinctCategoryIds = new List<long>();
            var uniqueCategoryIds = new HashSet<long>();

            foreach (var categoryId in categoryIds)
            {
                if (categoryId <= 0)
                {
                    continue;
                }

                if (uniqueCategoryIds.Add(categoryId))
                {
                    distinctCategoryIds.Add(categoryId);
                }
            }

            return distinctCategoryIds;
        }

        private static Product ToProductEntity(CreateProductDto dto)
        {
            return new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Stock = dto.Stock,
                Batch = dto.Batch,
                ExpirationDate = dto.ExpirationDate,
                Price = dto.Price
            };
        }
    }
}
