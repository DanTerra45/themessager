using ServicioCatalogo.Application.Categories.Models;
using ServicioCatalogo.Domain.Audit.Entities;
using ServicioCatalogo.Domain.Shared;

namespace ServicioCatalogo.Application.Categories.Ports.Input
{
    public interface ICategoryManagementUseCase
    {
        Task<Result<IReadOnlyList<CategoryModel>>> GetPageByCursorAsync(int pageSize, string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default);
        Task<Result<IReadOnlyList<CategoryModel>>> GetPageFromAnchorAsync(int pageSize, string sortBy, string sortDirection, long anchorCategoryId, string searchTerm, CancellationToken cancellationToken = default);
        Task<Result<bool>> HasCategoriesByCursorAsync(string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default);
        Task<Result<string>> GetNextCategoryCodePreviewAsync(CancellationToken cancellationToken = default);
        Task<Result<UpdateCategoryDto>> GetForEditAsync(long categoryId, CancellationToken cancellationToken = default);
        Task<Result> CreateAsync(CreateCategoryDto newCategory, AuditActor actor, CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(UpdateCategoryDto editCategory, AuditActor actor, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(long categoryId, AuditActor actor, CancellationToken cancellationToken = default);
    }
}

