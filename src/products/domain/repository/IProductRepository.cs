using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mercadito
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<IEnumerable<ProductWithCategoriesModel>> GetAllProductsWithCategoriesAsync();
        Task<IEnumerable<ProductWithCategoriesModel>> GetProductsWithCategoriesByPages(int page);
        Task<IEnumerable<ProductWithCategoriesModel>> GetProductsWithCategoriesFilterByCategoryByPages(int page, long categoryId);
        Task<IEnumerable<Product>> GetProductsByPages(int page);
        Task<Product?> GetProductByIdAsync(long id);
        Task<long> AddProductAsync(CreateProductDto product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(long id);
        Task<int> GetTotalProductsCountAsync();
        Task<int> GetTotalProductsCountByCategoryAsync(long categoryId);
    }
}