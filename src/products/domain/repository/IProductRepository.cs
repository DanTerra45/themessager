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
        Task<IEnumerable<Product>> GetProductsByPages(int page);
        Task<Product?> GetProductByIdAsync(Guid id);
        Task<Guid> AddProductAsync(CreateProductDto product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(Guid id);
    }
}