using MSProducto.Domain.Entities;
using MSProducto.Domain.Common;

namespace MSProducto.Domain.Ports.Input
{
    public interface ICategoriaService
    {
        Task<IReadOnlyList<Categoria>> GetPageByCursorAsync(int pageSize, string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Categoria>> GetPageFromAnchorAsync(int pageSize, string sortBy, string sortDirection, long anchorCategoryId, string searchTerm, CancellationToken cancellationToken = default);
        Task<bool> HasCategoriesByCursorAsync(string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default);
        Task<string> GetNextCategoryCodePreviewAsync(CancellationToken cancellationToken = default);
        Task<Categoria?> GetForEditAsync(long categoryId, CancellationToken cancellationToken = default);
        Task<Result<long>> CreateAsync(Categoria newCategory, AuditActor actor, CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(Categoria editCategory, AuditActor actor, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long categoryId, AuditActor actor, CancellationToken cancellationToken = default);
    }
}