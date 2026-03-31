using Mercadito.src.categories.domain.dto;
using Mercadito.src.categories.domain.model;

namespace Mercadito.src.categories.domain.usecases
{
    public interface ICategoryManagementUseCase
    {
        Task<IReadOnlyList<CategoryModel>> GetPageByCursorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorCategoryId,
            bool isNextPage,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CategoryModel>> GetPageFromAnchorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long anchorCategoryId,
            CancellationToken cancellationToken = default);
        Task<bool> HasCategoriesByCursorAsync(
            string sortBy,
            string sortDirection,
            long cursorCategoryId,
            bool isNextPage,
            CancellationToken cancellationToken = default);
        Task<string> GetNextCategoryCodePreviewAsync(CancellationToken cancellationToken = default);
        Task<UpdateCategoryDto?> GetForEditAsync(long categoryId, CancellationToken cancellationToken = default);
        Task CreateAsync(CreateCategoryDto newCategory, CancellationToken cancellationToken = default);
        Task UpdateAsync(UpdateCategoryDto editCategory, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long categoryId, CancellationToken cancellationToken = default);
    }
}
