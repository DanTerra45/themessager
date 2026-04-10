using Mercadito.src.audit.application.services;
using Mercadito.src.audit.domain.entities;
using Mercadito.src.categories.application.models;
using Mercadito.src.categories.application.ports.input;
using Mercadito.src.categories.application.ports.output;
using Mercadito.src.categories.application.validation;
using Mercadito.src.shared.domain;
using System.ComponentModel.DataAnnotations;
using Mercadito.src.shared.domain.exceptions;

namespace Mercadito.src.categories.application.usecases
{
    public class CategoryManagementUseCase(
        ICategoryRepository categoryRepository,
        ICreateCategoryValidator createCategoryValidator,
        IUpdateCategoryValidator updateCategoryValidator,
        IAuditTrailService auditTrailService) : ICategoryManagementUseCase
    {
        public async Task<IReadOnlyList<CategoryModel>> GetPageByCursorAsync(int pageSize, string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default)
        {
            return await categoryRepository.GetCategoriesByCursorAsync(pageSize, sortBy, sortDirection, cursorCategoryId, isNextPage, searchTerm, cancellationToken);
        }

        public async Task<IReadOnlyList<CategoryModel>> GetPageFromAnchorAsync(int pageSize, string sortBy, string sortDirection, long anchorCategoryId, string searchTerm, CancellationToken cancellationToken = default)
        {
            return await categoryRepository.GetCategoriesFromAnchorAsync(pageSize, sortBy, sortDirection, anchorCategoryId, searchTerm, cancellationToken);
        }

        public async Task<bool> HasCategoriesByCursorAsync(string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default)
        {
            return await categoryRepository.HasCategoriesByCursorAsync(sortBy, sortDirection, cursorCategoryId, isNextPage, searchTerm, cancellationToken);
        }

        public async Task<string> GetNextCategoryCodePreviewAsync(CancellationToken cancellationToken = default)
        {
            return await categoryRepository.GetNextCategoryCodeAsync(cancellationToken);
        }

        public async Task<UpdateCategoryDto?> GetForEditAsync(long categoryId, CancellationToken cancellationToken = default)
        {
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

        public async Task<Result> CreateAsync(CreateCategoryDto newCategory, AuditActor actor, CancellationToken cancellationToken = default)
        {
            var actorValidation = auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return actorValidation;
            }

            var validationResult = createCategoryValidator.Validate(newCategory);
            if (validationResult.IsFailure)
            {
                if (validationResult.Errors.Count > 0)
                {
                    return Result.Failure(validationResult.Errors);
                }

                return Result.Failure(validationResult.ErrorMessage);
            }

            try
            {
                var categoryId = await categoryRepository.CreateAsync(validationResult.Value, cancellationToken);
                await auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Create,
                    "categorias",
                    categoryId,
                    null,
                    validationResult.Value,
                    cancellationToken);

                return Result.Success();
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure(validationException.Errors);
                }

                return Result.Failure(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result.Failure(validationException.Message);
            }
        }

        public async Task<Result> UpdateAsync(UpdateCategoryDto editCategory, AuditActor actor, CancellationToken cancellationToken = default)
        {
            var actorValidation = auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return actorValidation;
            }

            var validationResult = updateCategoryValidator.Validate(editCategory);
            if (validationResult.IsFailure)
            {
                if (validationResult.Errors.Count > 0)
                {
                    return Result.Failure(validationResult.Errors);
                }

                return Result.Failure(validationResult.ErrorMessage);
            }

            try
            {
                var previousCategory = await categoryRepository.GetByIdAsync(validationResult.Value.Id, cancellationToken);
                var affectedRows = await categoryRepository.UpdateAsync(validationResult.Value, cancellationToken);
                if (affectedRows == 0)
                {
                    return Result.Failure("Categoría no encontrada.");
                }

                await auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Update,
                    "categorias",
                    validationResult.Value.Id,
                    previousCategory,
                    validationResult.Value,
                    cancellationToken);

                return Result.Success();
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure(validationException.Errors);
                }

                return Result.Failure(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result.Failure(validationException.Message);
            }
        }

        public async Task<bool> DeleteAsync(long categoryId, AuditActor actor, CancellationToken cancellationToken = default)
        {
            var actorValidation = auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return false;
            }

            var previousCategory = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            var affectedRows = await categoryRepository.DeleteAsync(categoryId, cancellationToken);
            if (affectedRows > 0 && previousCategory != null)
            {
                await auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Delete,
                    "categorias",
                    categoryId,
                    previousCategory,
                    new { Estado = "I" },
                    cancellationToken);
            }

            return affectedRows > 0;
        }
    }
}
