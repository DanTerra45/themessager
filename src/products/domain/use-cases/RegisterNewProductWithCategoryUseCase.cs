using Mercadito.src.products.domain.dto;
using Mercadito.src.products.data.entity;
using Mercadito.src.products.domain.repository;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.products.domain.usecases
{
    public class RegisterNewProductWithCategoryUseCase(
        IProductRepository productRepository) : IRegisterNewProductWithCategoryUseCase
    {

        private readonly IProductRepository _productRepository = productRepository;

        public async Task ExecuteAsync(CreateProductDto newProduct)
        {
            try
            {
                await _productRepository.AddProductWithCategoryAsync(
                    ToProductEntity(newProduct),
                    newProduct.CategoryId);
            }
            catch(Exception exception)
            {
                throw new InvalidOperationException("Error al registrar producto con categoría", exception);
            }
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
