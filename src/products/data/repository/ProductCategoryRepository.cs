using Dapper;
using Mercadito.database.interfaces;
using Mercadito.src.products.data.entity;
using Mercadito.src.products.domain.repository;

namespace Mercadito.src.products.data.repository
{
    public class ProductCategoryRepository(IDataBaseConnection dbConnection) : IProductCategoryRepository
    {
        private readonly IDataBaseConnection _dbConnection = dbConnection;

        public async Task<IEnumerable<ProductCategory>> GetAllProductCategoriesAsync()
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            const string query = @"SELECT productId AS ProductId,
                        categoriaId AS CategoryId
                        FROM categoriaDeProducto";
            return await connection.QueryAsync<ProductCategory>(query);
        }

        public async Task<ProductCategory?> GetProductsCategoriesByProductIdAsync(long productId)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            const string query = @"SELECT productId AS ProductId,
                        categoriaId AS CategoryId
                        FROM categoriaDeProducto
                        WHERE productId = @ProductId";
            return await connection.QueryFirstOrDefaultAsync<ProductCategory>(query, new { productId });
        }

        public async Task<ProductCategory?> GetProductsCategoriesByCategoryIdAsync(long categoryId)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            const string query = @"SELECT productId AS ProductId,
                        categoriaId AS CategoryId
                        FROM categoriaDeProducto
                        WHERE categoriaId = @CategoryId";
            return await connection.QueryFirstOrDefaultAsync<ProductCategory>(query, new { categoryId });
        }

        public async Task AddProductCategoryAsync(ProductCategory productCategory)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            const string query = @"INSERT INTO categoriaDeProducto (productId, categoriaId)
                        VALUES (@ProductId, @CategoryId)";
            await connection.ExecuteAsync(query, new { productCategory.ProductId, productCategory.CategoryId });
        }

        public async Task DeleteProductCategoryAsync(ProductCategory productCategory)
        {
            throw new NotImplementedException("Pending external upload.");
        }

        public async Task DeleteProductCategoriesByProductIdAsync(long productId)
        {
            throw new NotImplementedException("Pending external upload.");
        }
    }
}
