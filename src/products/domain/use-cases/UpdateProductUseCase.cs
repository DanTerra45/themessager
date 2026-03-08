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

        public async Task ExecuteAsync(UpdateProductDto updateProduct)
        {
            var existingProduct = await _productRepository.GetProductByIdAsync(updateProduct.Id);
            if (existingProduct == null)
            {
                throw new ValidationException("Producto no encontrado.");
            }

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

            await _productRepository.UpdateProductWithCategoryAsync(productToUpdate, updateProduct.CategoryId);
        }
    }
}
