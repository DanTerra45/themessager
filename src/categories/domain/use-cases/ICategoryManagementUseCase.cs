using Mercadito.src.categories.domain.dto;
using Mercadito.src.categories.domain.model;

namespace Mercadito.src.categories.domain.usecases
{
    public interface ICategoryManagementUseCase
    {
        Task<(IReadOnlyList<CategoryModel> Categories, int TotalPages)> GetPageAsync(int currentPage, int pageSize);
        Task<UpdateCategoryDto?> GetForEditAsync(long categoryId);
        Task CreateAsync(CreateCategoryDto newCategory);
        Task UpdateAsync(UpdateCategoryDto editCategory);
        Task<bool> DeleteAsync(long categoryId);
    }
}
