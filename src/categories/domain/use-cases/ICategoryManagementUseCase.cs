using Mercadito.src.categories.domain.dto;
using Mercadito.src.categories.domain.model;
using Shared.Domain;
using System.Threading;
using System.Threading.Tasks;

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

        // Changed to Task<Result> to represent expected validation outcomes without exceptions
        Task<Result> CreateAsync(CreateCategoryDto newCategory, CancellationToken cancellationToken = default);

        // Changed to Task<Result> to represent expected validation outcomes without exceptions
        Task<Result> UpdateAsync(UpdateCategoryDto editCategory, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(long categoryId, CancellationToken cancellationToken = default);
    }
}
