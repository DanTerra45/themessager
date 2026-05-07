using Mercadito.Sales.Api.Application.Products.Models;
using Mercadito.Sales.Api.Application.Categories.Models;
using Mercadito.Sales.Api.Domain.Audit.Entities;
using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Products.Ports.Input
{
    public interface IProductManagementUseCase
    {
        Task<IReadOnlyList<CategoryModel>> GetCategoriesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductWithCategoriesModel>> GetPageByCursorAsync(long categoryFilter, int pageSize, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductWithCategoriesModel>> GetPageFromAnchorAsync(long categoryFilter, int pageSize, string sortBy, string sortDirection, long anchorProductId, string searchTerm = "", CancellationToken cancellationToken = default);
        Task<bool> HasProductsByCursorAsync(long categoryFilter, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default);
        Task<Result> CreateAsync(CreateProductDto newProduct, AuditActor actor, CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(UpdateProductDto updateProduct, AuditActor actor, CancellationToken cancellationToken = default);
        Task<UpdateProductDto?> GetForEditAsync(long productId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long productId, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
