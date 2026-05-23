using MSProducto.Domain.Entities;
using MSProducto.Domain.Common;

namespace MSProducto.Domain.Ports.Input
{
    public interface IProductoService
    {
        Task<IReadOnlyList<Categoria>> GetCategoriesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Producto>> GetPageByCursorAsync(long categoryFilter, int pageSize, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Producto>> GetPageFromAnchorAsync(long categoryFilter, int pageSize, string sortBy, string sortDirection, long anchorProductId, string searchTerm = "", CancellationToken cancellationToken = default);
        Task<bool> HasProductsByCursorAsync(long categoryFilter, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default);
        Task<Producto?> GetForEditAsync(long productId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long productId, AuditActor actor, CancellationToken cancellationToken = default);
        Task<Result> CreateAsync(Producto newProduct, AuditActor actor, CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(Producto updateProduct, AuditActor actor, CancellationToken cancellationToken = default);
    }
}