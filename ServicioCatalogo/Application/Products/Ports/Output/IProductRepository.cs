using ServicioCatalogo.Application.Products.Models;

namespace ServicioCatalogo.Application.Products.Ports.Output
{
    public interface IProductRepository
    {
        Task<IReadOnlyList<ProductWithCategoriesModel>> GetProductsWithCategoriesByCursorAsync(int pageSize, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductWithCategoriesModel>> GetProductsWithCategoriesByCategoryCursorAsync(long categoryId, int pageSize, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductWithCategoriesModel>> GetProductsWithCategoriesFromAnchorAsync(long categoryId, int pageSize, string sortBy, string sortDirection, long anchorProductId, string searchTerm = "", CancellationToken cancellationToken = default);
        Task<bool> HasProductsByCursorAsync(long categoryId, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default);
        Task<ProductForEditModel?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<long> CreateAsync(ProductWithCategoriesWriteModel productWithCategories, CancellationToken cancellationToken = default);
        Task<int> UpdateAsync(ProductWithCategoriesWriteModel productWithCategories, CancellationToken cancellationToken = default);
        Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}

