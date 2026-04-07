using Mercadito.src.categories.domain.model;
using Mercadito.src.categories.data.repository;
using Mercadito.src.products.domain.dto;
using Mercadito.src.products.domain.factory;
using Mercadito.src.products.domain.model;
using Mercadito.src.products.data.repository;
using Mercadito.src.shared.domain.factory;
using Shared.Domain;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mercadito.src.products.domain.usecases
{
    public class ProductManagementUseCase(
        RepositoryCreator<ProductRepository> productRepositoryCreator,
        RepositoryCreator<CategoryRepository> categoryRepositoryCreator,
        IProductFactory productFactory) : IProductManagementUseCase
    {
        private readonly RepositoryCreator<ProductRepository> _productRepositoryCreator = productRepositoryCreator;
        private readonly RepositoryCreator<CategoryRepository> _categoryRepositoryCreator = categoryRepositoryCreator;
        private readonly IProductFactory _productFactory = productFactory;

        public async Task<IReadOnlyList<CategoryModel>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var categoryRepository = _categoryRepositoryCreator.Create();
            var categories = await categoryRepository.GetAllCategoriesAsync(cancellationToken);
            return categories;
        }

        public async Task<IReadOnlyList<ProductWithCategoriesModel>> GetPageByCursorAsync(
            long categoryFilter,
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorProductId,
            bool isNextPage,
            string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            if (cursorProductId <= 0)
            {
                var productRepository = _productRepositoryCreator.Create();
                var anchorProducts = await productRepository.GetProductsWithCategoriesFromAnchorAsync(
                    categoryFilter,
                    pageSize,
                    sortBy,
                    sortDirection,
                    anchorProductId: 0,
                    searchTerm,
                    cancellationToken);
                return anchorProducts;
            }

            var productRepositoryForCursor = _productRepositoryCreator.Create();

            if (categoryFilter == 0)
            {
                var products = await productRepositoryForCursor.GetProductsWithCategoriesByCursorAsync(
                    pageSize,
                    sortBy,
                    sortDirection,
                    cursorProductId,
                    isNextPage,
                    searchTerm,
                    cancellationToken);
                return products;
            }

            var filteredProducts = await productRepositoryForCursor.GetProductsWithCategoriesByCategoryCursorAsync(
                categoryFilter,
                pageSize,
                sortBy,
                sortDirection,
                cursorProductId,
                isNextPage,
                searchTerm,
                cancellationToken);

            return filteredProducts;
        }

        public async Task<IReadOnlyList<ProductWithCategoriesModel>> GetPageFromAnchorAsync(
            long categoryFilter,
            int pageSize,
            string sortBy,
            string sortDirection,
            long anchorProductId,
            string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            var productRepository = _productRepositoryCreator.Create();
            return await productRepository.GetProductsWithCategoriesFromAnchorAsync(
                categoryFilter,
                pageSize,
                sortBy,
                sortDirection,
                anchorProductId,
                searchTerm,
                cancellationToken);
        }

        public async Task<bool> HasProductsByCursorAsync(
            long categoryFilter,
            string sortBy,
            string sortDirection,
            long cursorProductId,
            bool isNextPage,
            string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            var productRepository = _productRepositoryCreator.Create();
            return await productRepository.HasProductsByCursorAsync(
                categoryFilter,
                sortBy,
                sortDirection,
                cursorProductId,
                isNextPage,
                searchTerm,
                cancellationToken);
        }

        public async Task<UpdateProductDto?> GetForEditAsync(long productId, CancellationToken cancellationToken = default)
        {
            var productRepository = _productRepositoryCreator.Create();
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

        public async Task<bool> DeleteAsync(long productId, CancellationToken cancellationToken = default)
        {
            var productRepository = _productRepositoryCreator.Create();
            var affectedRows = await productRepository.DeleteAsync(productId, cancellationToken);
            return affectedRows > 0;
        }

        public async Task<Result> CreateAsync(CreateProductDto newProduct, CancellationToken cancellationToken = default)
        {
            var productRepository = _productRepositoryCreator.Create();
            var product = _productFactory.CreateForInsert(newProduct);
            var writeModel = new ProductWithCategoriesWriteModel
            {
                Product = product,
                CategoryIds = newProduct.CategoryIds // Use CategoryIds from the DTO, not Product
            };

            var productId = await productRepository.CreateAsync(writeModel, cancellationToken);
            return productId > 0 ? Result.Success() : Result.Failure("Failed to create product.");
        }

        public async Task<Result> UpdateAsync(UpdateProductDto updateProduct, CancellationToken cancellationToken = default)
        {
            var productRepository = _productRepositoryCreator.Create();
            var product = _productFactory.CreateForUpdate(updateProduct);
            var writeModel = new ProductWithCategoriesWriteModel
            {
                Product = product,
                CategoryIds = updateProduct.CategoryIds // Use CategoryIds from the DTO, not Product
            };

            var affectedRows = await productRepository.UpdateAsync(writeModel, cancellationToken);
            return affectedRows > 0 ? Result.Success() : Result.Failure("Failed to update product.");
        }
    }
}
