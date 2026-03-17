using Mercadito.src.products.domain.dto;
using Mercadito.src.products.domain.factory;
using Mercadito.src.products.domain.model;
using Mercadito.src.products.data.repository;
using Mercadito.src.shared.domain.factory;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.products.domain.usecases
{
    public class UpdateProductUseCase(
        IProductFactory productFactory,
        RepositoryCreator<ProductRepository> productRepositoryCreator) : IUpdateProductUseCase
    {

        private readonly IProductFactory _productFactory = productFactory;
        private readonly RepositoryCreator<ProductRepository> _productRepositoryCreator = productRepositoryCreator;

        public async Task ExecuteAsync(UpdateProductDto updateProduct, CancellationToken cancellationToken = default)
        {
            var productRepository = _productRepositoryCreator.Create();
            var productToUpdate = _productFactory.CreateForUpdate(updateProduct);

            var affectedRows = await productRepository.UpdateAsync(
                new ProductWithCategoriesWriteModel
                {
                    Product = productToUpdate,
                    CategoryIds = updateProduct.CategoryIds
                },
                cancellationToken);

            if (affectedRows == 0)
            {
                throw new ValidationException("Producto no encontrado.");
            }
        }
    }
}
