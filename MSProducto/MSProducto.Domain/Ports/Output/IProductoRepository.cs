using MSProducto.Domain.Entities;

namespace MSProducto.Domain.Ports.Output
{
    public interface IProductoRepository
    {
        Task<IReadOnlyList<Producto>> GetProductsWithCategoriesByCursorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorProductId,
            bool isNextPage,
            string searchTerm = "",
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Producto>> GetProductsWithCategoriesByCategoryCursorAsync(
            long categoryId,
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorProductId,
            bool isNextPage,
            string searchTerm = "",
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Producto>> GetProductsWithCategoriesFromAnchorAsync(
            long categoryId,
            int pageSize,
            string sortBy,
            string sortDirection,
            long anchorProductId,
            string searchTerm = "",
            CancellationToken cancellationToken = default);
        Task<bool> HasProductsByCursorAsync(
            long categoryId,
            string sortBy,
            string sortDirection,
            long cursorProductId,
            bool isNextPage,
            string searchTerm = "",
            CancellationToken cancellationToken = default);
        Task<Producto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<long> CreateAsync(Producto product, CancellationToken cancellationToken = default);
        Task<int> UpdateAsync(Producto product, CancellationToken cancellationToken = default);
        Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}