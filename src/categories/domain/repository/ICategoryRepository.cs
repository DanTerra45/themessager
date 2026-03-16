using Mercadito.src.categories.data.entity;
using Mercadito.src.categories.domain.model;

namespace Mercadito.src.categories.domain.repository
{
    public interface ICategoryRepository
    {
        Task<IReadOnlyList<CategoryModel>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
        Task<int> GetTotalCategoriesCountAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CategoryModel>> GetCategoryByPages(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<CategoryModel?> GetCategoryByIdAsync(long id, CancellationToken cancellationToken = default);
        Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default);
        Task<int> UpdateCategoryAsync(Category category, CancellationToken cancellationToken = default);
        Task<int> DeleteCategoryAsync(long id, CancellationToken cancellationToken = default);
    }
}
