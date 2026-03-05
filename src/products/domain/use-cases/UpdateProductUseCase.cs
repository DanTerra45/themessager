using System;

using Dapper;
using Mercadito.src.products.data.dto;

namespace Mercadito
{
    
    public class UpdateProductUseCase
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductCategoryRepository _productCategoryRepository;
        private readonly AsignCategoryToProductUseCase _assignCategoryToProductUseCase;
        public UpdateProductUseCase(IProductRepository productRepository, ICategoryRepository categoryRepository, AsignCategoryToProductUseCase assignCategoryToProductUseCase, IProductCategoryRepository productCategoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _assignCategoryToProductUseCase = assignCategoryToProductUseCase;
            _productCategoryRepository = productCategoryRepository;
        }

        public async Task ExecuteAsync(UpdateProductDto updateProduct)
        {
            var existing = await _productRepository.GetProductByIdAsync(updateProduct.Id);
            var updated = new Product
            {
             Id = updateProduct.Id,
            Name = updateProduct.Name,
                Description = updateProduct.Description ?? string.Empty,
                Stock = updateProduct.Stock,
                Lote = updateProduct.Lote,
                FechaDeCaducidad = updateProduct.FechaDeCaducidad,
                Price = updateProduct.Price
            };

            await _productRepository.UpdateProductAsync(updated);

                // Manejar relación producto-categoría
            var existingRelation = await _productCategoryRepository.GetProductsCategoriesByProductIdAsync(updated.Id);
            if (existingRelation != null && existingRelation.CategoryId != updateProduct.CategoryId)
            {
                await _productCategoryRepository.DeleteProductCategoryAsync(existingRelation);
            }
            if (updateProduct.CategoryId != 0)
            {
                var relationNow = await _productCategoryRepository.GetProductsCategoriesByProductIdAsync(updated.Id);
                if (relationNow == null || relationNow.CategoryId != updateProduct.CategoryId)
                {
                    var newRel = new ProductCategory(updated.Id, updateProduct.CategoryId);
                    await _productCategoryRepository.AddProductCategoryAsync(newRel);
                }
            }
        }
    }
}