using Mercadito.src.audit.application.services;
using Mercadito.src.audit.domain.entities;
using Mercadito.src.categories.application.models;
using Mercadito.src.products.application.models;
using Mercadito.src.products.application.ports.input;
using Mercadito.src.products.application.ports.output;
using Mercadito.src.products.application.validation;
using Shared.Domain;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.products.application.use_cases
{
    public class ProductManagementUseCase : IProductManagementUseCase
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductCategoryLookupRepository _categoryLookupRepository;
        private readonly ICreateProductValidator _createProductValidator;
        private readonly IUpdateProductValidator _updateProductValidator;
        private readonly IAuditTrailService _auditTrailService;

        public ProductManagementUseCase(
            IProductRepository productRepository,
            IProductCategoryLookupRepository categoryLookupRepository,
            ICreateProductValidator createProductValidator,
            IUpdateProductValidator updateProductValidator,
            IAuditTrailService auditTrailService)
        {
            _productRepository = productRepository;
            _categoryLookupRepository = categoryLookupRepository;
            _createProductValidator = createProductValidator;
            _updateProductValidator = updateProductValidator;
            _auditTrailService = auditTrailService;
        }

        public async Task<IReadOnlyList<CategoryModel>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return await _categoryLookupRepository.GetAllCategoriesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ProductWithCategoriesModel>> GetPageByCursorAsync(long categoryFilter, int pageSize, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default)
        {
            if (cursorProductId <= 0)
            {
                return await _productRepository.GetProductsWithCategoriesFromAnchorAsync(categoryFilter, pageSize, sortBy, sortDirection, 0, searchTerm, cancellationToken);
            }

            if (categoryFilter == 0)
            {
                return await _productRepository.GetProductsWithCategoriesByCursorAsync(pageSize, sortBy, sortDirection, cursorProductId, isNextPage, searchTerm, cancellationToken);
            }

            return await _productRepository.GetProductsWithCategoriesByCategoryCursorAsync(categoryFilter, pageSize, sortBy, sortDirection, cursorProductId, isNextPage, searchTerm, cancellationToken);
        }

        public async Task<IReadOnlyList<ProductWithCategoriesModel>> GetPageFromAnchorAsync(long categoryFilter, int pageSize, string sortBy, string sortDirection, long anchorProductId, string searchTerm = "", CancellationToken cancellationToken = default)
        {
            return await _productRepository.GetProductsWithCategoriesFromAnchorAsync(categoryFilter, pageSize, sortBy, sortDirection, anchorProductId, searchTerm, cancellationToken);
        }

        public async Task<bool> HasProductsByCursorAsync(long categoryFilter, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default)
        {
            return await _productRepository.HasProductsByCursorAsync(categoryFilter, sortBy, sortDirection, cursorProductId, isNextPage, searchTerm, cancellationToken);
        }

        public async Task<UpdateProductDto?> GetForEditAsync(long productId, CancellationToken cancellationToken = default)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
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
            var actorValidation = _auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return false;
            }

            var previousProduct = await _productRepository.GetByIdAsync(productId, cancellationToken);
            var affectedRows = await _productRepository.DeleteAsync(productId, cancellationToken);
            if (affectedRows > 0 && previousProduct != null)
            {
                await _auditTrailService.RecordAsync(
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
            var actorValidation = _auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return actorValidation;
            }

            var validationResult = _createProductValidator.Validate(newProduct);
            if (validationResult.IsFailure)
            {
                return validationResult.Errors.Count > 0
                    ? Result.Failure(validationResult.Errors)
                    : Result.Failure(validationResult.ErrorMessage);
            }

            var writeModel = new ProductWithCategoriesWriteModel
            {
                Product = validationResult.Value,
                CategoryIds = validationResult.Value.CategoryIds
            };

            try
            {
                var productId = await _productRepository.CreateAsync(writeModel, cancellationToken);
                if (productId > 0)
                {
                    await _auditTrailService.RecordAsync(
                        actor,
                        AuditAction.Create,
                        "products",
                        productId,
                        null,
                        validationResult.Value,
                        cancellationToken);
                }

                return productId > 0 ? Result.Success() : Result.Failure("Failed to create product.");
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

        public async Task<Result> UpdateAsync(UpdateProductDto updateProduct, AuditActor actor, CancellationToken cancellationToken = default)
        {
            var actorValidation = _auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return actorValidation;
            }

            var validationResult = _updateProductValidator.Validate(updateProduct);
            if (validationResult.IsFailure)
            {
                return validationResult.Errors.Count > 0
                    ? Result.Failure(validationResult.Errors)
                    : Result.Failure(validationResult.ErrorMessage);
            }

            var writeModel = new ProductWithCategoriesWriteModel
            {
                Product = validationResult.Value,
                CategoryIds = validationResult.Value.CategoryIds
            };
            var previousProduct = await _productRepository.GetByIdAsync(validationResult.Value.Id, cancellationToken);

            try
            {
                var affectedRows = await _productRepository.UpdateAsync(writeModel, cancellationToken);
                if (affectedRows > 0)
                {
                    await _auditTrailService.RecordAsync(
                        actor,
                        AuditAction.Update,
                        "products",
                        validationResult.Value.Id,
                        previousProduct,
                        validationResult.Value,
                        cancellationToken);
                }

                return affectedRows > 0 ? Result.Success() : Result.Failure("Failed to update product.");
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
    }
}
