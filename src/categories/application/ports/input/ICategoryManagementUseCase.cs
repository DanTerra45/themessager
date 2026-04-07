using Mercadito.src.categories.application.models;
using Mercadito.src.audit.domain.entities;
using Shared.Domain;

namespace Mercadito.src.categories.application.ports.input
{
    public interface ICategoryManagementUseCase
    {
        Task<IReadOnlyList<CategoryModel>> GetPageByCursorAsync(int pageSize, string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CategoryModel>> GetPageFromAnchorAsync(int pageSize, string sortBy, string sortDirection, long anchorCategoryId, CancellationToken cancellationToken = default);
        Task<bool> HasCategoriesByCursorAsync(string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, CancellationToken cancellationToken = default);
        Task<string> GetNextCategoryCodePreviewAsync(CancellationToken cancellationToken = default);
        Task<UpdateCategoryDto?> GetForEditAsync(long categoryId, CancellationToken cancellationToken = default);
        Task<Result> CreateAsync(CreateCategoryDto newCategory, AuditActor actor, CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(UpdateCategoryDto editCategory, AuditActor actor, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long categoryId, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
