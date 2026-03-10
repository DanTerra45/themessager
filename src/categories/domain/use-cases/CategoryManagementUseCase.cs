using System.ComponentModel.DataAnnotations;
using System.Linq;
using Mercadito.src.categories.domain.dto;
using Mercadito.src.categories.data.entity;
using Mercadito.src.categories.domain.model;
using Mercadito.src.categories.domain.repository;

namespace Mercadito.src.categories.domain.usecases
{
    public class CategoryManagementUseCase(ICategoryRepository categoryRepository) : ICategoryManagementUseCase
    {

        private readonly ICategoryRepository _categoryRepository = categoryRepository;

        public async Task<(IReadOnlyList<CategoryModel> Categories, int TotalPages)> GetPageAsync(int currentPage, int pageSize)
        {
            var totalCount = await _categoryRepository.GetTotalCategoriesCountAsync();
            var totalPages = CalculateTotalPages(totalCount, pageSize);
            var pagedCategories = (await _categoryRepository.GetCategoryByPages(currentPage, pageSize)).ToList();
            return (pagedCategories, totalPages);
        }

        private static int CalculateTotalPages(int totalItems, int pageSize)
        {
            if (totalItems == 0 || pageSize <= 0) return 1;
            return (totalItems + pageSize - 1) / pageSize;
        }

        public async Task<UpdateCategoryDto?> GetForEditAsync(long categoryId)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(categoryId);
            if (category == null)
            {
                return null;
            }

            return new UpdateCategoryDto
            {
                Id = category.Id,
                Code = category.Code,
                Name = category.Name,
                Description = category.Description
            };
        }

        public async Task CreateAsync(CreateCategoryDto newCategory)
        {
            var category = new Category
            {
                Code = newCategory.Code,
                Name = newCategory.Name,
                Description = newCategory.Description
            };

            await _categoryRepository.AddCategoryAsync(category);
        }

        public async Task UpdateAsync(UpdateCategoryDto editCategory)
        {
            throw new NotImplementedException("Pending external upload.");
        }

        public async Task<bool> DeleteAsync(long categoryId)
        {
            throw new NotImplementedException("Pending external upload.");
        }
    }
}
