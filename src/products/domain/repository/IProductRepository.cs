using Mercadito.src.products.data.entity;
using Mercadito.src.products.domain.model;

namespace Mercadito.src.products.domain.repository
{
    public interface IProductRepository
    {
        Task<IEnumerable<ProductWithCategoriesModel>> GetProductsWithCategoriesByPages(int page, int pageSize);
        Task<IEnumerable<ProductWithCategoriesModel>> GetProductsWithCategoriesFilterByCategoryByPages(int page, long categoryId, int pageSize);
        Task<Product?> GetProductByIdAsync(long id);
        Task<ProductForEditModel?> GetProductForEditAsync(long id);
        Task<long> AddProductWithCategoryAsync(Product product, long categoryId);
        Task UpdateProductWithCategoryAsync(Product product, long categoryId);
        Task<int> DeleteProductAsync(long id);
        Task<int> GetTotalProductsCountAsync();
        Task<int> GetTotalProductsCountByCategoryAsync(long categoryId);
    }
}
