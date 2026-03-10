using Mercadito.src.categories.domain.model;
using Mercadito.src.categories.domain.repository;
using Mercadito.src.products.domain.dto;
using Mercadito.src.products.domain.model;
using Mercadito.src.products.domain.repository;

namespace Mercadito.src.products.domain.usecases
{
    public class ProductManagementUseCase(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository) : IProductManagementUseCase
    {

        private readonly IProductRepository _productRepository = productRepository;
        private readonly ICategoryRepository _categoryRepository = categoryRepository;

        public async Task<IReadOnlyList<CategoryModel>> GetCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllCategoriesAsync();
            return [.. categories];
        }

        public async Task<(IReadOnlyList<ProductWithCategoriesModel> Products, int TotalPages)> GetPageAsync(int currentPage, long categoryFilter, int pageSize)
        {
            if (categoryFilter == 0)
            {
                var totalCount = await _productRepository.GetTotalProductsCountAsync();
                var totalPages = CalculateTotalPages(totalCount, pageSize);
                var products = await _productRepository.GetProductsWithCategoriesByPages(currentPage, pageSize);
                return ([.. products], totalPages);
            }

            var filteredTotalCount = await _productRepository.GetTotalProductsCountByCategoryAsync(categoryFilter);
            var filteredTotalPages = CalculateTotalPages(filteredTotalCount, pageSize);
            var filteredProducts = await _productRepository.GetProductsWithCategoriesFilterByCategoryByPages(currentPage, categoryFilter, pageSize);
            return ([.. filteredProducts], filteredTotalPages);
        }

        private static int CalculateTotalPages(int totalItems, int pageSize)
        {
            if (totalItems == 0 || pageSize <= 0) return 1;
            return (totalItems + pageSize - 1) / pageSize;
        }

        public async Task<UpdateProductDto?> GetForEditAsync(long productId)
        {
            var product = await _productRepository.GetProductForEditAsync(productId);
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
                CategoryId = product.CategoryId
            };
        }

        public async Task<bool> DeleteAsync(long productId)
        {
            throw new NotImplementedException("Pending external upload.");
        }
    }
}
