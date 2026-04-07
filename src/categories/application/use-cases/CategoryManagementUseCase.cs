using Mercadito.src.audit.application.services;
using Mercadito.src.audit.domain.entities;
using Mercadito.src.categories.application.models;
using Mercadito.src.categories.application.ports.input;
using Mercadito.src.categories.application.ports.output;
using Mercadito.src.categories.application.validation;
using Shared.Domain;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.categories.application.use_cases
{
    public class CategoryManagementUseCase : ICategoryManagementUseCase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICreateCategoryValidator _createCategoryValidator;
        private readonly IUpdateCategoryValidator _updateCategoryValidator;
        private readonly IAuditTrailService _auditTrailService;

        public CategoryManagementUseCase(
            ICategoryRepository categoryRepository,
            ICreateCategoryValidator createCategoryValidator,
            IUpdateCategoryValidator updateCategoryValidator,
            IAuditTrailService auditTrailService)
        {
            _categoryRepository = categoryRepository;
            _createCategoryValidator = createCategoryValidator;
            _updateCategoryValidator = updateCategoryValidator;
            _auditTrailService = auditTrailService;
        }

        public async Task<IReadOnlyList<CategoryModel>> GetPageByCursorAsync(int pageSize, string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default)
        {
            return await _categoryRepository.GetCategoriesByCursorAsync(pageSize, sortBy, sortDirection, cursorCategoryId, isNextPage, searchTerm, cancellationToken);
        }

        public async Task<IReadOnlyList<CategoryModel>> GetPageFromAnchorAsync(int pageSize, string sortBy, string sortDirection, long anchorCategoryId, string searchTerm, CancellationToken cancellationToken = default)
        {
            return await _categoryRepository.GetCategoriesFromAnchorAsync(pageSize, sortBy, sortDirection, anchorCategoryId, searchTerm, cancellationToken);
        }

        public async Task<bool> HasCategoriesByCursorAsync(string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default)
        {
            return await _categoryRepository.HasCategoriesByCursorAsync(sortBy, sortDirection, cursorCategoryId, isNextPage, searchTerm, cancellationToken);
        }

        public async Task<string> GetNextCategoryCodePreviewAsync(CancellationToken cancellationToken = default)
        {
            return await _categoryRepository.GetNextCategoryCodeAsync(cancellationToken);
        }

        public async Task<UpdateCategoryDto?> GetForEditAsync(long categoryId, CancellationToken cancellationToken = default)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
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
            var actorValidation = _auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return actorValidation;
            }

            var validationResult = _createCategoryValidator.Validate(newCategory);
            if (validationResult.IsFailure)
            {
                return validationResult.Errors.Count > 0
                    ? Result.Failure(validationResult.Errors)
                    : Result.Failure(validationResult.ErrorMessage);
            }

            try
            {
                var categoryId = await _categoryRepository.CreateAsync(validationResult.Value, cancellationToken);
                await _auditTrailService.RecordAsync(
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
                return validationException.Errors.Count > 0
                    ? Result.Failure(validationException.Errors)
                    : Result.Failure(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result.Failure(validationException.Message);
            }
        }

        public async Task<Result> UpdateAsync(UpdateCategoryDto editCategory, AuditActor actor, CancellationToken cancellationToken = default)
        {
            var actorValidation = _auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return actorValidation;
            }

            var validationResult = _updateCategoryValidator.Validate(editCategory);
            if (validationResult.IsFailure)
            {
                return validationResult.Errors.Count > 0
                    ? Result.Failure(validationResult.Errors)
                    : Result.Failure(validationResult.ErrorMessage);
            }

            try
            {
                var previousCategory = await _categoryRepository.GetByIdAsync(validationResult.Value.Id, cancellationToken);
                var affectedRows = await _categoryRepository.UpdateAsync(validationResult.Value, cancellationToken);
                if (affectedRows == 0)
                {
                    return Result.Failure("Categoría no encontrada.");
                }

                await _auditTrailService.RecordAsync(
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
                return validationException.Errors.Count > 0
                    ? Result.Failure(validationException.Errors)
                    : Result.Failure(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result.Failure(validationException.Message);
            }
        }

        public async Task<bool> DeleteAsync(long categoryId, AuditActor actor, CancellationToken cancellationToken = default)
        {
            var actorValidation = _auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return false;
            }

            var previousCategory = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            var affectedRows = await _categoryRepository.DeleteAsync(categoryId, cancellationToken);
            if (affectedRows > 0 && previousCategory != null)
            {
                await _auditTrailService.RecordAsync(
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
