using MSProducto.Domain.Entities;

namespace MSProducto.Domain.Ports.Output
{
    public interface ICategoriaRepository
    {
        Task<IReadOnlyList<Categoria>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
        Task<string> GetNextCategoryCodeAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Categoria>> GetCategoriesByCursorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorCategoryId,
            bool isNextPage,
            string searchTerm,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Categoria>> GetCategoriesFromAnchorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long anchorCategoryId,
            string searchTerm,
            CancellationToken cancellationToken = default);
        Task<bool> HasCategoriesByCursorAsync(
            string sortBy,
            string sortDirection,
            long cursorCategoryId,
            bool isNextPage,
            string searchTerm,
            CancellationToken cancellationToken = default);
        Task<Categoria?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<long> CreateAsync(Categoria category, CancellationToken cancellationToken = default);
        Task<int> UpdateAsync(Categoria category, CancellationToken cancellationToken = default);
        Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}