using Mercadito.src.categories.domain.model;
using Mercadito.src.categories.data.repository;
using Mercadito.src.products.domain.dto;
using Mercadito.src.products.domain.model;
using Mercadito.src.products.data.repository;
using Mercadito.src.shared.domain.factory;

namespace Mercadito.src.products.domain.usecases
{
    public class ProductManagementUseCase(
        RepositoryCreator<ProductRepository> productRepositoryCreator,
        RepositoryCreator<CategoryRepository> categoryRepositoryCreator) : IProductManagementUseCase
    {
        private readonly RepositoryCreator<ProductRepository> _productRepositoryCreator = productRepositoryCreator;
        private readonly RepositoryCreator<CategoryRepository> _categoryRepositoryCreator = categoryRepositoryCreator;

        public async Task<IReadOnlyList<CategoryModel>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var categoryRepository = _categoryRepositoryCreator.Create();
            var categories = await categoryRepository.GetAllCategoriesAsync(cancellationToken);
            return categories;
        }

        public async Task<(IReadOnlyList<ProductWithCategoriesModel> Products, int TotalPages)> GetPageAsync(
            int currentPage,
            long categoryFilter,
            int pageSize,
            string sortBy,
            string sortDirection,
            CancellationToken cancellationToken = default)
        {
            var productRepository = _productRepositoryCreator.Create();

            if (categoryFilter == 0)
            {
                var totalCount = await productRepository.GetTotalProductsCountAsync(cancellationToken);
                var totalPages = CalculateTotalPages(totalCount, pageSize);
                var products = await productRepository.GetProductsWithCategoriesByPages(currentPage, pageSize, sortBy, sortDirection, cancellationToken);
                return (products, totalPages);
            }

            var filteredTotalCount = await productRepository.GetTotalProductsCountByCategoryAsync(categoryFilter, cancellationToken);
            var filteredTotalPages = CalculateTotalPages(filteredTotalCount, pageSize);
            var filteredProducts = await productRepository.GetProductsWithCategoriesFilterByCategoryByPages(
                currentPage,
                categoryFilter,
                pageSize,
                sortBy,
                sortDirection,
                cancellationToken);
            return (filteredProducts, filteredTotalPages);
        }

        private static int CalculateTotalPages(int totalItems, int pageSize)
        {
            if (totalItems == 0 || pageSize <= 0) return 1;
            return (totalItems + pageSize - 1) / pageSize;
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
    }
}
