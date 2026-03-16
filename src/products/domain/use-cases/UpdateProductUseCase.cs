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
                Name = NormalizeRequired(updateProduct.Name),
                Description = NormalizeRequired(updateProduct.Description),
                Stock = updateProduct.Stock,
                Batch = NormalizeRequired(updateProduct.Batch),
                ExpirationDate = updateProduct.ExpirationDate,
                Price = updateProduct.Price
            };

            var affectedRows = await _productRepository.UpdateProductWithCategoriesAsync(productToUpdate, updateProduct.CategoryIds, cancellationToken);
            if (affectedRows == 0)
            {
                throw new ValidationException("Producto no encontrado.");
            }
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
