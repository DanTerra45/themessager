using System.ComponentModel.DataAnnotations;
using Mercadito.src.categories.domain.dto;
using Mercadito.src.categories.domain.factory;
using Mercadito.src.categories.domain.model;
using Mercadito.src.categories.domain.repository;

namespace Mercadito.src.categories.domain.usecases
{
    public class CategoryManagementUseCase(
        ICategoryRepository categoryRepository,
        ICategoryFactory categoryFactory) : ICategoryManagementUseCase
    {
        private readonly ICategoryRepository _categoryRepository = categoryRepository;
        private readonly ICategoryFactory _categoryFactory = categoryFactory;

        public async Task<(IReadOnlyList<CategoryModel> Categories, int TotalPages)> GetPageAsync(
            int currentPage,
            int pageSize,
            string sortBy,
            string sortDirection,
            CancellationToken cancellationToken = default)
        {
            var totalCount = await _categoryRepository.GetTotalCategoriesCountAsync(cancellationToken);
            var totalPages = CalculateTotalPages(totalCount, pageSize);
            var pagedCategories = await _categoryRepository.GetCategoryByPages(currentPage, pageSize, sortBy, sortDirection, cancellationToken);
            return (pagedCategories, totalPages);
        }

        public async Task<UpdateCategoryDto?> GetForEditAsync(long categoryId, CancellationToken cancellationToken = default)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(categoryId, cancellationToken);
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

        public async Task CreateAsync(CreateCategoryDto newCategory, CancellationToken cancellationToken = default)
        {
            var category = _categoryFactory.CreateForInsert(newCategory);
            await _categoryRepository.AddCategoryAsync(category, cancellationToken);
        }

        public async Task UpdateAsync(UpdateCategoryDto editCategory, CancellationToken cancellationToken = default)
        {
            var category = _categoryFactory.CreateForUpdate(editCategory);
            var affectedRows = await _categoryRepository.UpdateCategoryAsync(category, cancellationToken);

            if (affectedRows == 0)
            {
                throw new ValidationException("Categoria no encontrada.");
            }
        }

        public async Task<bool> DeleteAsync(long categoryId, CancellationToken cancellationToken = default)
        {
            var affectedRows = await _categoryRepository.DeleteCategoryAsync(categoryId, cancellationToken);
            return affectedRows > 0;
        }

        private static int CalculateTotalPages(int totalItems, int pageSize)
        {
            if (totalItems == 0 || pageSize <= 0)
            {
                return 1;
            }

            return (totalItems + pageSize - 1) / pageSize;
        }
    }
}
