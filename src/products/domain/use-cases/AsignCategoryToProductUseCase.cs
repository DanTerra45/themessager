using System;

using Dapper;

namespace Mercadito
{
    
    public class AsignCategoryToProductUseCase
    {
        private readonly IProductCategoryRepository _productCategoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public AsignCategoryToProductUseCase(IProductCategoryRepository productCategoryRepository, IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productCategoryRepository = productCategoryRepository;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task ExecuteAsync(long productId, long categoryId)
        {
            var product = await _productRepository.GetProductByIdAsync(productId);
            var category = await _categoryRepository.GetCategoryByIdAsync(categoryId);

            if (product == null || category == null)
            {
                throw new Exception("Producto o categoría no encontrados.");
            }

            var productCategory = new ProductCategory(productId, categoryId);
            await _productCategoryRepository.AddProductCategoryAsync(productCategory);
        }
    }
}