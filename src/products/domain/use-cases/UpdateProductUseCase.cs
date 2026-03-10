using Mercadito.src.products.domain.dto;
using Mercadito.src.products.data.entity;
using Mercadito.src.products.domain.repository;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.products.domain.usecases
{
    public class UpdateProductUseCase(
        IProductRepository productRepository) : IUpdateProductUseCase
    {

        private readonly IProductRepository _productRepository = productRepository;

        public async Task ExecuteAsync(UpdateProductDto updateProduct, CancellationToken cancellationToken = default)
        {
            var productToUpdate = new Product
            {
                Id = updateProduct.Id,
                Name = updateProduct.Name,
                Description = updateProduct.Description,
                Stock = updateProduct.Stock,
                Batch = updateProduct.Batch,
                ExpirationDate = updateProduct.ExpirationDate,
                Price = updateProduct.Price
            };

            var categoryIds = GetDistinctCategoryIds(updateProduct.CategoryIds);
            var affectedRows = await _productRepository.UpdateProductWithCategoriesAsync(productToUpdate, categoryIds, cancellationToken);
            if (affectedRows == 0)
            {
                throw new ValidationException("Producto no encontrado.");
            }
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
    }
}
