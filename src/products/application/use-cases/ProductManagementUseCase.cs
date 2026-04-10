using Mercadito.src.audit.application.services;
using Mercadito.src.audit.domain.entities;
using Mercadito.src.categories.application.models;
using Mercadito.src.products.application.models;
using Mercadito.src.products.application.ports.input;
using Mercadito.src.products.application.ports.output;
using Mercadito.src.products.application.validation;
using Mercadito.src.shared.domain;
using System.ComponentModel.DataAnnotations;
using Mercadito.src.shared.domain.exceptions;

namespace Mercadito.src.products.application.usecases
{
    public class ProductManagementUseCase(
        IProductRepository productRepository,
        IProductCategoryLookupRepository categoryLookupRepository,
        ICreateProductValidator createProductValidator,
        IUpdateProductValidator updateProductValidator,
        IAuditTrailService auditTrailService) : IProductManagementUseCase
    {
        public async Task<IReadOnlyList<CategoryModel>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return await categoryLookupRepository.GetAllCategoriesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ProductWithCategoriesModel>> GetPageByCursorAsync(long categoryFilter, int pageSize, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default)
        {
            if (cursorProductId <= 0)
            {
                return await productRepository.GetProductsWithCategoriesFromAnchorAsync(categoryFilter, pageSize, sortBy, sortDirection, 0, searchTerm, cancellationToken);
            }

            if (categoryFilter == 0)
            {
                return await productRepository.GetProductsWithCategoriesByCursorAsync(pageSize, sortBy, sortDirection, cursorProductId, isNextPage, searchTerm, cancellationToken);
            }

            return await productRepository.GetProductsWithCategoriesByCategoryCursorAsync(categoryFilter, pageSize, sortBy, sortDirection, cursorProductId, isNextPage, searchTerm, cancellationToken);
        }

        public async Task<IReadOnlyList<ProductWithCategoriesModel>> GetPageFromAnchorAsync(long categoryFilter, int pageSize, string sortBy, string sortDirection, long anchorProductId, string searchTerm = "", CancellationToken cancellationToken = default)
        {
            return await productRepository.GetProductsWithCategoriesFromAnchorAsync(categoryFilter, pageSize, sortBy, sortDirection, anchorProductId, searchTerm, cancellationToken);
        }

        public async Task<bool> HasProductsByCursorAsync(long categoryFilter, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default)
        {
            return await productRepository.HasProductsByCursorAsync(categoryFilter, sortBy, sortDirection, cursorProductId, isNextPage, searchTerm, cancellationToken);
        }

        public async Task<UpdateProductDto?> GetForEditAsync(long productId, CancellationToken cancellationToken = default)
        {
            var product = await productRepository.GetByIdAsync(productId, cancellationToken);
            if (product == null)
            {
                return null;
            }

            return new UpdateProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Stock = product.Stock,
                Batch = product.Batch,
                ExpirationDate = product.ExpirationDate,
                Price = product.Price,
                CategoryIds = [.. product.CategoryIds]
            };
        }

        public async Task<bool> DeleteAsync(long productId, AuditActor actor, CancellationToken cancellationToken = default)
        {
            var actorValidation = auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return false;
            }

            var previousProduct = await productRepository.GetByIdAsync(productId, cancellationToken);
            var affectedRows = await productRepository.DeleteAsync(productId, cancellationToken);
            if (affectedRows > 0 && previousProduct != null)
            {
                await auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Delete,
                    "products",
                    productId,
                    previousProduct,
                    new { Estado = "I" },
                    cancellationToken);
            }

            return affectedRows > 0;
        }

        public async Task<Result> CreateAsync(CreateProductDto newProduct, AuditActor actor, CancellationToken cancellationToken = default)
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

            try
            {
                var productId = await productRepository.CreateAsync(writeModel, cancellationToken);
                if (productId > 0)
                {
                    await auditTrailService.RecordAsync(
                        actor,
                        AuditAction.Create,
                        "products",
                        productId,
                        null,
                        validationResult.Value,
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
                return Result.Failure(validationException.Message);
            }
        }

        public async Task<Result> UpdateAsync(UpdateProductDto updateProduct, AuditActor actor, CancellationToken cancellationToken = default)
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

            try
            {
                var affectedRows = await productRepository.UpdateAsync(writeModel, cancellationToken);
                if (affectedRows > 0)
                {
                    await auditTrailService.RecordAsync(
                        actor,
                        AuditAction.Update,
                        "products",
                        validationResult.Value.Id,
                        previousProduct,
                        validationResult.Value,
                        cancellationToken);
                }

                if (affectedRows > 0)
                {
                    return Result.Success();
                }

                return Result.Failure("Failed to update product.");
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
    }
}
