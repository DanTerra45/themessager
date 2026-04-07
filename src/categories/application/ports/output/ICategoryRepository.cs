using Mercadito.src.categories.application.models;
using Mercadito.src.categories.domain.entities;

namespace Mercadito.src.categories.application.ports.output
{
    public interface ICategoryRepository
    {
        Task<IReadOnlyList<CategoryModel>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
        Task<string> GetNextCategoryCodeAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CategoryModel>> GetCategoriesByCursorAsync(int pageSize, string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CategoryModel>> GetCategoriesFromAnchorAsync(int pageSize, string sortBy, string sortDirection, long anchorCategoryId, CancellationToken cancellationToken = default);
        Task<bool> HasCategoriesByCursorAsync(string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, CancellationToken cancellationToken = default);
        Task<CategoryModel?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<long> CreateAsync(Category category, CancellationToken cancellationToken = default);
        Task<int> UpdateAsync(Category category, CancellationToken cancellationToken = default);
        Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
