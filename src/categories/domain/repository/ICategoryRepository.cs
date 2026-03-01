using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mercadito
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<CategoryModel>> GetAllCategoriesAsync();
        Task<IEnumerable<CategoryModel>> GetCategoryByPages(int page);
        Task<CategoryModel?> GetCategoryByIdAsync(Guid id);
        Task AddCategoryAsync(CreateCategoryDto category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(Guid id);
    }
}