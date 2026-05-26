using ServicioCatalogo.Application.Products.Models;
using ServicioCatalogo.Application.Categories.Models;
using ServicioCatalogo.Domain.Audit.Entities;
using ServicioCatalogo.Domain.Shared;

namespace ServicioCatalogo.Application.Products.Ports.Input
{
    public interface IProductManagementUseCase
    {
        Task<Result<IReadOnlyList<CategoryModel>>> GetCategoriesAsync(CancellationToken cancellationToken = default);
        Task<Result<IReadOnlyList<ProductWithCategoriesModel>>> GetPageByCursorAsync(long categoryFilter, int pageSize, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default);
        Task<Result<IReadOnlyList<ProductWithCategoriesModel>>> GetPageFromAnchorAsync(long categoryFilter, int pageSize, string sortBy, string sortDirection, long anchorProductId, string searchTerm = "", CancellationToken cancellationToken = default);
        Task<Result<bool>> HasProductsByCursorAsync(long categoryFilter, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default);
        Task<Result> CreateAsync(CreateProductDto newProduct, AuditActor actor, CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(UpdateProductDto updateProduct, AuditActor actor, CancellationToken cancellationToken = default);
        Task<Result<UpdateProductDto>> GetForEditAsync(long productId, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(long productId, AuditActor actor, CancellationToken cancellationToken = default);
    }
}

