using System;

using Dapper;
namespace Mercadito
{
    public interface IProductCategoryRepository
    {
        Task<IEnumerable<ProductCategory>> GetAllProductCategoriesAsync();
        Task<ProductCategory?> GetProductsCategoriesByProductIdAsync(Guid productId);
        Task<ProductCategory?> GetProductsCategoriesByCategoryIdAsync(Guid categoryId);
        Task AddProductCategoryAsync(ProductCategory productCategory);
        Task DeleteProductCategoryAsync(ProductCategory productCategory);
    }
}