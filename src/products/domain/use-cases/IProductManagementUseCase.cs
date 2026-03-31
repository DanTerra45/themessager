using Mercadito.src.categories.domain.model;
using Mercadito.src.products.domain.dto;
using Mercadito.src.products.domain.model;

namespace Mercadito.src.products.domain.usecases
{
    public interface IProductManagementUseCase
    {
        Task<IReadOnlyList<CategoryModel>> GetCategoriesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductWithCategoriesModel>> GetPageByCursorAsync(
            long categoryFilter,
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorProductId,
            bool isNextPage,
            string searchTerm = "",
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductWithCategoriesModel>> GetPageFromAnchorAsync(
            long categoryFilter,
            int pageSize,
            string sortBy,
            string sortDirection,
            long anchorProductId,
            string searchTerm = "",
            CancellationToken cancellationToken = default);
        Task<bool> HasProductsByCursorAsync(
            long categoryFilter,
            string sortBy,
            string sortDirection,
            long cursorProductId,
            bool isNextPage,
            string searchTerm = "",
            CancellationToken cancellationToken = default);
        Task CreateAsync(CreateProductDto newProduct, CancellationToken cancellationToken = default);
        Task UpdateAsync(UpdateProductDto updateProduct, CancellationToken cancellationToken = default);
        Task<UpdateProductDto?> GetForEditAsync(long productId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long productId, CancellationToken cancellationToken = default);
    }
}
