using System.ComponentModel.DataAnnotations;
using Mercadito.src.categories.data.repository;
using Mercadito.src.categories.domain.dto;
using Mercadito.src.categories.domain.factory;
using Mercadito.src.categories.domain.model;
using Mercadito.src.shared.domain.factory;

namespace Mercadito.src.categories.domain.usecases
{
    public class CategoryManagementUseCase(
        RepositoryCreator<CategoryRepository> categoryRepositoryCreator,
        ICategoryFactory categoryFactory) : ICategoryManagementUseCase
    {
        private readonly RepositoryCreator<CategoryRepository> _categoryRepositoryCreator = categoryRepositoryCreator;
        private readonly ICategoryFactory _categoryFactory = categoryFactory;

        public async Task<(IReadOnlyList<CategoryModel> Categories, int TotalPages)> GetPageAsync(
            int currentPage,
            int pageSize,
            string sortBy,
            string sortDirection,
            CancellationToken cancellationToken = default)
        {
            var categoryRepository = _categoryRepositoryCreator.Create();
            var totalCount = await categoryRepository.GetTotalCategoriesCountAsync(cancellationToken);
            var totalPages = CalculateTotalPages(totalCount, pageSize);
            var pagedCategories = await categoryRepository.GetCategoryByPages(currentPage, pageSize, sortBy, sortDirection, cancellationToken);
            return (pagedCategories, totalPages);
        }

        public async Task<UpdateCategoryDto?> GetForEditAsync(long categoryId, CancellationToken cancellationToken = default)
        {
            var categoryRepository = _categoryRepositoryCreator.Create();
            var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
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
            var categoryRepository = _categoryRepositoryCreator.Create();
            var category = _categoryFactory.CreateForInsert(newCategory);
            await categoryRepository.CreateAsync(category, cancellationToken);
        }

        public async Task UpdateAsync(UpdateCategoryDto editCategory, CancellationToken cancellationToken = default)
        {
            var categoryRepository = _categoryRepositoryCreator.Create();
            var category = _categoryFactory.CreateForUpdate(editCategory);
            var affectedRows = await categoryRepository.UpdateAsync(category, cancellationToken);

            if (affectedRows == 0)
            {
                throw new ValidationException("Categoria no encontrada.");
            }
        }

        public async Task<bool> DeleteAsync(long categoryId, CancellationToken cancellationToken = default)
        {
            var categoryRepository = _categoryRepositoryCreator.Create();
            var affectedRows = await categoryRepository.DeleteAsync(categoryId, cancellationToken);
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
