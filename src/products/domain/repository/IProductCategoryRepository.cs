using Mercadito.src.products.data.entity;

namespace Mercadito.src.products.domain.repository
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
