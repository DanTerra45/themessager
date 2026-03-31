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

        public async Task<IReadOnlyList<CategoryModel>> GetPageByCursorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorCategoryId,
            bool isNextPage,
            CancellationToken cancellationToken = default)
        {
            var categoryRepository = _categoryRepositoryCreator.Create();
            return await categoryRepository.GetCategoriesByCursorAsync(
                pageSize,
                sortBy,
                sortDirection,
                cursorCategoryId,
                isNextPage,
                cancellationToken);
        }

        public async Task<IReadOnlyList<CategoryModel>> GetPageFromAnchorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long anchorCategoryId,
            CancellationToken cancellationToken = default)
        {
            var categoryRepository = _categoryRepositoryCreator.Create();
            return await categoryRepository.GetCategoriesFromAnchorAsync(
                pageSize,
                sortBy,
                sortDirection,
                anchorCategoryId,
                cancellationToken);
        }

        public async Task<bool> HasCategoriesByCursorAsync(
            string sortBy,
            string sortDirection,
            long cursorCategoryId,
            bool isNextPage,
            CancellationToken cancellationToken = default)
        {
            var categoryRepository = _categoryRepositoryCreator.Create();
            return await categoryRepository.HasCategoriesByCursorAsync(
                sortBy,
                sortDirection,
                cursorCategoryId,
                isNextPage,
                cancellationToken);
        }

        public async Task<string> GetNextCategoryCodePreviewAsync(CancellationToken cancellationToken = default)
        {
            var categoryRepository = _categoryRepositoryCreator.Create();
            return await categoryRepository.GetNextCategoryCodeAsync(cancellationToken);
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
            var existingCategory = await categoryRepository.GetByIdAsync(editCategory.Id, cancellationToken);
            if (existingCategory == null)
            {
                throw new ValidationException("Categoría no encontrada.");
            }

            var categoryToUpdate = new UpdateCategoryDto
            {
                Id = editCategory.Id,
                Code = editCategory.Code,
                Name = editCategory.Name,
                Description = editCategory.Description
            };

            var category = _categoryFactory.CreateForUpdate(categoryToUpdate);
            var affectedRows = await categoryRepository.UpdateAsync(category, cancellationToken);

            if (affectedRows == 0)
            {
                throw new ValidationException("Categoría no encontrada.");
            }
        }

        public async Task<bool> DeleteAsync(long categoryId, CancellationToken cancellationToken = default)
        {
            var categoryRepository = _categoryRepositoryCreator.Create();
            var affectedRows = await categoryRepository.DeleteAsync(categoryId, cancellationToken);
            return affectedRows > 0;
        }

    }
}
