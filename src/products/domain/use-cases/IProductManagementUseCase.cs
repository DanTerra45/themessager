using Mercadito.src.categories.domain.model;
using Mercadito.src.products.domain.dto;
using Mercadito.src.products.domain.model;

namespace Mercadito.src.products.domain.usecases
{
    public interface IProductManagementUseCase
    {
        Task<IReadOnlyList<CategoryModel>> GetCategoriesAsync(CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<ProductWithCategoriesModel> Products, int TotalPages)> GetPageAsync(int currentPage, long categoryFilter, int pageSize, CancellationToken cancellationToken = default);
        Task<UpdateProductDto?> GetForEditAsync(long productId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long productId, CancellationToken cancellationToken = default);
    }
}
