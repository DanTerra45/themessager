using System.ComponentModel.DataAnnotations;
using Mercadito.src.categories.data.repository;
using Mercadito.src.categories.domain.dto;
using Mercadito.src.categories.domain.factory;
using Mercadito.src.categories.domain.model;
using Mercadito.src.shared.domain.factory;
using Shared.Domain;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Mercadito.src.categories.data.entity;

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

        public async Task<Result> CreateAsync(CreateCategoryDto newCategory, CancellationToken cancellationToken = default)
        {
            Category category;
            try
            {
                category = _categoryFactory.CreateForInsert(newCategory);
            }
            catch (ValidationException ex)
            {
                return Result.Failure(ex.Message);
            }

            var categoryRepository = _categoryRepositoryCreator.Create();

            await categoryRepository.CreateAsync(category, cancellationToken);
            return Result.Success();
        }

        public async Task<Result> UpdateAsync(UpdateCategoryDto editCategory, CancellationToken cancellationToken = default)
        {
            var categoryRepository = _categoryRepositoryCreator.Create();
            var existingCategory = await categoryRepository.GetByIdAsync(editCategory.Id, cancellationToken);
            if (existingCategory == null)
            {
                return Result.Failure("Categoría no encontrada.");
            }

            Category categoryToUpdate;
            try
            {
                categoryToUpdate = _categoryFactory.CreateForUpdate(editCategory);
            }
            catch (ValidationException ex)
            {
                return Result.Failure(ex.Message);
            }

            var affectedRows = await categoryRepository.UpdateAsync(categoryToUpdate, cancellationToken);
            if (affectedRows == 0)
            {
                return Result.Failure("Categoría no encontrada.");
            }

            return Result.Success();
        }

        public async Task<bool> DeleteAsync(long categoryId, CancellationToken cancellationToken = default)
        {
            var categoryRepository = _categoryRepositoryCreator.Create();
            var affectedRows = await categoryRepository.DeleteAsync(categoryId, cancellationToken);
            return affectedRows > 0;
        }

    }
}
