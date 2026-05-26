using ServicioCatalogo.Application.Audit.Services;
using ServicioCatalogo.Domain.Audit.Entities;
using ServicioCatalogo.Application.Categories.Models;
using ServicioCatalogo.Application.Products.Models;
using ServicioCatalogo.Application.Products.Ports.Input;
using ServicioCatalogo.Application.Products.Ports.Output;
using ServicioCatalogo.Application.Products.Validation;
using ServicioCatalogo.Domain.Products.Entities;
using ServicioCatalogo.Domain.Shared;
using System.ComponentModel.DataAnnotations;
using ServicioCatalogo.Domain.Shared.Exceptions;

namespace ServicioCatalogo.Application.Products.UseCases
{
    public class ProductManagementUseCase(
        IProductRepository productRepository,
        IProductCategoryLookupRepository categoryLookupRepository,
        ICreateProductValidator createProductValidator,
        IUpdateProductValidator updateProductValidator,
        IAuditTrailService auditTrailService) : IProductManagementUseCase
    {
        public async Task<Result<IReadOnlyList<CategoryModel>>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var categories = await categoryLookupRepository.GetAllCategoriesAsync(cancellationToken);
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

        public async Task<Result<IReadOnlyList<ProductWithCategoriesModel>>> GetPageByCursorAsync(long categoryFilter, int pageSize, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default)
        {
            try
            {
                if (cursorProductId <= 0)
                {
                    var anchorRows = await productRepository.GetProductsWithCategoriesFromAnchorAsync(categoryFilter, pageSize, sortBy, sortDirection, 0, searchTerm, cancellationToken);
                    return Result.Success<IReadOnlyList<ProductWithCategoriesModel>>(anchorRows);
                }

                if (categoryFilter == 0)
                {
                    var rows = await productRepository.GetProductsWithCategoriesByCursorAsync(pageSize, sortBy, sortDirection, cursorProductId, isNextPage, searchTerm, cancellationToken);
                    return Result.Success<IReadOnlyList<ProductWithCategoriesModel>>(rows);
                }

                var filteredRows = await productRepository.GetProductsWithCategoriesByCategoryCursorAsync(categoryFilter, pageSize, sortBy, sortDirection, cursorProductId, isNextPage, searchTerm, cancellationToken);
                return Result.Success<IReadOnlyList<ProductWithCategoriesModel>>(filteredRows);
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<IReadOnlyList<ProductWithCategoriesModel>>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure<IReadOnlyList<ProductWithCategoriesModel>>();
            }
        }

        public async Task<Result<IReadOnlyList<ProductWithCategoriesModel>>> GetPageFromAnchorAsync(long categoryFilter, int pageSize, string sortBy, string sortDirection, long anchorProductId, string searchTerm = "", CancellationToken cancellationToken = default)
        {
            try
            {
                var rows = await productRepository.GetProductsWithCategoriesFromAnchorAsync(categoryFilter, pageSize, sortBy, sortDirection, anchorProductId, searchTerm, cancellationToken);
                return Result.Success<IReadOnlyList<ProductWithCategoriesModel>>(rows);
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<IReadOnlyList<ProductWithCategoriesModel>>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure<IReadOnlyList<ProductWithCategoriesModel>>();
            }
        }

        public async Task<Result<bool>> HasProductsByCursorAsync(long categoryFilter, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default)
        {
            try
            {
                var hasRows = await productRepository.HasProductsByCursorAsync(categoryFilter, sortBy, sortDirection, cursorProductId, isNextPage, searchTerm, cancellationToken);
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

        public async Task<Result<UpdateProductDto>> GetForEditAsync(long productId, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await productRepository.GetByIdAsync(productId, cancellationToken);
                if (product == null)
                {
                    return Result.Failure<UpdateProductDto>(new Dictionary<string, List<string>>
                    {
                        { "NotFound", ["Producto no encontrado."] }
                    });
                }

                return Result.Success(new UpdateProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Stock = product.Stock,
                    Batch = product.Batch,
                    ExpirationDate = product.ExpirationDate,
                    Price = product.Price,
                    CategoryIds = [.. product.CategoryIds]
                });
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<UpdateProductDto>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure<UpdateProductDto>();
            }
        }

        public async Task<Result> DeleteAsync(long productId, AuditActor actor, CancellationToken cancellationToken = default)
        {
            try
            {
                var actorValidation = auditTrailService.ValidateActor(actor);
                if (actorValidation.IsFailure)
                {
                    return actorValidation;
                }

                var previousProduct = await productRepository.GetByIdAsync(productId, cancellationToken);
                var affectedRows = await productRepository.DeleteAsync(productId, actor.UserId, cancellationToken);
                if (affectedRows > 0 && previousProduct != null)
                {
                    await auditTrailService.RecordAsync(
                        actor,
                        AuditAction.Delete,
                        "products",
                        productId,
                        ProductAuditSnapshotFactory.BuildDeletedSnapshot(previousProduct),
                        null,
                        cancellationToken);
                }

                if (affectedRows == 0)
                {
                    return Result.Failure(new Dictionary<string, List<string>>
                    {
                        { "NotFound", ["El producto no existe o ya estaba desactivado."] }
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

        public async Task<Result> CreateAsync(CreateProductDto newProduct, AuditActor actor, CancellationToken cancellationToken = default)
        {
            try
            {
                var actorValidation = auditTrailService.ValidateActor(actor);
                if (actorValidation.IsFailure)
                {
                    return actorValidation;
                }

                var validationResult = createProductValidator.Validate(newProduct);
                if (validationResult.IsFailure)
                {
                    if (validationResult.Errors.Count > 0)
                    {
                        return Result.Failure(validationResult.Errors);
                    }

                    return Result.Failure(validationResult.ErrorMessage);
                }

                var writeModel = new ProductWithCategoriesWriteModel
                {
                    Product = validationResult.Value,
                    CategoryIds = validationResult.Value.CategoryIds
                };

                var productId = await productRepository.CreateAsync(writeModel, actor.UserId, cancellationToken);
                if (productId > 0)
                {
                    await auditTrailService.RecordAsync(
                        actor,
                        AuditAction.Create,
                        "products",
                        productId,
                        null,
                        ProductAuditSnapshotFactory.BuildCreatedSnapshot(validationResult.Value),
                        cancellationToken);
                }

                if (productId > 0)
                {
                    return Result.Success();
                }

                return Result.Failure("Failed to create product.");
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

        public async Task<Result> UpdateAsync(UpdateProductDto updateProduct, AuditActor actor, CancellationToken cancellationToken = default)
        {
            try
            {
                var actorValidation = auditTrailService.ValidateActor(actor);
                if (actorValidation.IsFailure)
                {
                    return actorValidation;
                }

                var validationResult = updateProductValidator.Validate(updateProduct);
                if (validationResult.IsFailure)
                {
                    if (validationResult.Errors.Count > 0)
                    {
                        return Result.Failure(validationResult.Errors);
                    }

                    return Result.Failure(validationResult.ErrorMessage);
                }

                var writeModel = new ProductWithCategoriesWriteModel
                {
                    Product = validationResult.Value,
                    CategoryIds = validationResult.Value.CategoryIds
                };
                var previousProduct = await productRepository.GetByIdAsync(validationResult.Value.Id, cancellationToken);

                var affectedRows = await productRepository.UpdateAsync(writeModel, actor.UserId, cancellationToken);
                if (affectedRows > 0)
                {
                    var auditSnapshot = ProductAuditSnapshotFactory.BuildImportantUpdateSnapshot(previousProduct, validationResult.Value);
                    if (auditSnapshot.HasValue)
                    {
                        await auditTrailService.RecordAsync(
                            actor,
                            AuditAction.Update,
                            "products",
                            validationResult.Value.Id,
                            auditSnapshot.Value.PreviousData,
                            auditSnapshot.Value.NewData,
                            cancellationToken);
                    }
                }

                if (affectedRows > 0)
                {
                    return Result.Success();
                }

                return Result.Failure(new Dictionary<string, List<string>>
                {
                    { "NotFound", ["Producto no encontrado."] }
                });
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

