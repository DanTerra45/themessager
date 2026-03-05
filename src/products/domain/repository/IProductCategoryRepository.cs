using System;

using Dapper;
namespace Mercadito
{
    public interface IProductCategoryRepository
    {
        Task<IEnumerable<ProductCategory>> GetAllProductCategoriesAsync();
        Task<ProductCategory?> GetProductsCategoriesByProductIdAsync(long productId);
        Task<ProductCategory?> GetProductsCategoriesByCategoryIdAsync(long categoryId);
        Task AddProductCategoryAsync(ProductCategory productCategory);
        Task DeleteProductCategoryAsync(ProductCategory productCategory);
        Task DeleteProductCategoriesByProductIdAsync(long productId);
    }
}