using Mercadito.src.categories.data.entity;
using Mercadito.src.categories.domain.model;

namespace Mercadito.src.categories.domain.repository
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<CategoryModel>> GetAllCategoriesAsync();
        Task<int> GetTotalCategoriesCountAsync();
        Task<IEnumerable<CategoryModel>> GetCategoryByPages(int page, int pageSize);
        Task<CategoryModel?> GetCategoryByIdAsync(long id);
        Task AddCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task<int> DeleteCategoryAsync(long id);
    }
}
