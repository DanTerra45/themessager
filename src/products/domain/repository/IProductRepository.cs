using Mercadito.src.products.data.entity;
using Mercadito.src.products.domain.model;

namespace Mercadito.src.products.domain.repository
{
    public interface IProductRepository
    {
        Task<IReadOnlyList<ProductWithCategoriesModel>> GetProductsWithCategoriesByPages(
            int page,
            int pageSize,
            string sortBy,
            string sortDirection,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductWithCategoriesModel>> GetProductsWithCategoriesFilterByCategoryByPages(
            int page,
            long categoryId,
            int pageSize,
            string sortBy,
            string sortDirection,
            CancellationToken cancellationToken = default);
        Task<ProductForEditModel?> GetProductForEditAsync(long id, CancellationToken cancellationToken = default);
        Task<long> AddProductWithCategoriesAsync(Product product, IReadOnlyList<long> categoryIds, CancellationToken cancellationToken = default);
        Task<int> UpdateProductWithCategoriesAsync(Product product, IReadOnlyList<long> categoryIds, CancellationToken cancellationToken = default);
        Task<int> DeleteProductAsync(long id, CancellationToken cancellationToken = default);
        Task<int> GetTotalProductsCountAsync(CancellationToken cancellationToken = default);
        Task<int> GetTotalProductsCountByCategoryAsync(long categoryId, CancellationToken cancellationToken = default);
    }
}
