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
        Task<CategoryModel?> GetCategoryByIdAsync(long id);
        Task AddCategoryAsync(CreateCategoryDto category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(long id);
    }
}