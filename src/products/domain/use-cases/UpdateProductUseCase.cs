using Mercadito.src.products.domain.dto;
using Mercadito.src.products.domain.factory;
using Mercadito.src.products.domain.repository;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.products.domain.usecases
{
    public class UpdateProductUseCase(
        IProductFactory productFactory,
        IProductRepository productRepository) : IUpdateProductUseCase
    {

        private readonly IProductFactory _productFactory = productFactory;
        private readonly IProductRepository _productRepository = productRepository;

        public async Task ExecuteAsync(UpdateProductDto updateProduct, CancellationToken cancellationToken = default)
        {
            var productToUpdate = _productFactory.CreateForUpdate(updateProduct);

            var affectedRows = await _productRepository.UpdateProductWithCategoriesAsync(productToUpdate, updateProduct.CategoryIds, cancellationToken);
            if (affectedRows == 0)
            {
                throw new ValidationException("Producto no encontrado.");
            }
        }
    }
}
