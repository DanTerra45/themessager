using ServicioCatalogo.Domain.Audit.Entities;
using ServicioCatalogo.Application.Categories.Models;
using ServicioCatalogo.Application.Categories.Ports.Input;
using ServicioCatalogo.Application.Categories.Ports.Output;
using ServicioCatalogo.Application.Categories.Validation;
using ServicioCatalogo.Domain.Shared;
using System.ComponentModel.DataAnnotations;
using ServicioCatalogo.Domain.Shared.Exceptions;

namespace ServicioCatalogo.Application.Categories.UseCases
{
    public class CategoryManagementUseCase(
        ICategoryRepository categoryRepository,
        ICreateCategoryValidator createCategoryValidator,
        IUpdateCategoryValidator updateCategoryValidator) : ICategoryManagementUseCase
    {
        public async Task<Result<IReadOnlyList<CategoryModel>>> GetPageByCursorAsync(int pageSize, string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default)
        {
            try
            {
                var categories = await categoryRepository.GetCategoriesByCursorAsync(pageSize, sortBy, sortDirection, cursorCategoryId, isNextPage, searchTerm, cancellationToken);
                return Result.Success<IReadOnlyList<CategoryModel>>(categories);
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<IReadOnlyList<CategoryModel>>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure<IReadOnlyList<CategoryModel>>();
            }
        }

        public async Task<Result<IReadOnlyList<CategoryModel>>> GetPageFromAnchorAsync(int pageSize, string sortBy, string sortDirection, long anchorCategoryId, string searchTerm, CancellationToken cancellationToken = default)
        {
            try
            {
                var categories = await categoryRepository.GetCategoriesFromAnchorAsync(pageSize, sortBy, sortDirection, anchorCategoryId, searchTerm, cancellationToken);
                return Result.Success<IReadOnlyList<CategoryModel>>(categories);
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<IReadOnlyList<CategoryModel>>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure<IReadOnlyList<CategoryModel>>();
            }
        }

        public async Task<Result<bool>> HasCategoriesByCursorAsync(string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default)
        {
            try
            {
                var hasRows = await categoryRepository.HasCategoriesByCursorAsync(sortBy, sortDirection, cursorCategoryId, isNextPage, searchTerm, cancellationToken);
                return Result.Success(hasRows);
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<bool>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure<bool>();
            }
        }

        public async Task<Result<string>> GetNextCategoryCodePreviewAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var code = await categoryRepository.GetNextCategoryCodeAsync(cancellationToken);
                return Result.Success(code);
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<string>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure<string>();
            }
        }

        public async Task<Result<UpdateCategoryDto>> GetForEditAsync(long categoryId, CancellationToken cancellationToken = default)
        {
            try
            {
                var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
                if (category == null)
                {
                    return Result.Failure<UpdateCategoryDto>(new Dictionary<string, List<string>>
                    {
                        { "NotFound", ["Categoría no encontrada."] }
                    });
                }

                return Result.Success(new UpdateCategoryDto
                {
                    Id = category.Id,
                    Code = category.Code,
                    Name = category.Name,
                    Description = category.Description
                });
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<UpdateCategoryDto>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure<UpdateCategoryDto>();
            }
        }

        public async Task<Result> CreateAsync(CreateCategoryDto newCategory, AuditActor actor, CancellationToken cancellationToken = default)
        {
            if (actor == null)
            {
                return Result.Failure(new Dictionary<string, List<string>>
                {
                    { "Validation", ["No se pudo resolver el actor de auditoría."] }
                });
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
                await categoryRepository.CreateAsync(validationResult.Value, actor.UserId, cancellationToken);
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
                return Result.Failure(new Dictionary<string, List<string>>
                {
                    { "Validation", [validationException.Message] }
                });
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure();
            }
        }

        public async Task<Result> UpdateAsync(UpdateCategoryDto editCategory, AuditActor actor, CancellationToken cancellationToken = default)
        {
            if (actor == null)
            {
                return Result.Failure(new Dictionary<string, List<string>>
                {
                    { "Validation", ["No se pudo resolver el actor de auditoría."] }
                });
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
                var affectedRows = await categoryRepository.UpdateAsync(validationResult.Value, actor.UserId, cancellationToken);
                if (affectedRows == 0)
                {
                    return Result.Failure(new Dictionary<string, List<string>>
                    {
                        { "NotFound", ["Categoría no encontrada."] }
                    });
                }

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
                return Result.Failure(new Dictionary<string, List<string>>
                {
                    { "Validation", [validationException.Message] }
                });
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure();
            }
        }

        public async Task<Result> DeleteAsync(long categoryId, AuditActor actor, CancellationToken cancellationToken = default)
        {
            if (actor == null)
            {
                return Result.Failure(new Dictionary<string, List<string>>
                {
                    { "Validation", ["No se pudo resolver el actor de auditoría."] }
                });
            }

            try
            {
                var affectedRows = await categoryRepository.DeleteAsync(categoryId, actor.UserId, cancellationToken);
                if (affectedRows == 0)
                {
                    return Result.Failure(new Dictionary<string, List<string>>
                    {
                        { "NotFound", ["La categoría no existe o ya estaba desactivada."] }
                    });
                }

                return Result.Success();
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure();
            }
        }

        private static Result UnexpectedFailure()
        {
            return Result.Failure("Se produjo un error inesperado al procesar la solicitud.");
        }

        private static Result<T> UnexpectedFailure<T>()
        {
            return Result.Failure<T>("Se produjo un error inesperado al procesar la solicitud.");
        }
    }
}

