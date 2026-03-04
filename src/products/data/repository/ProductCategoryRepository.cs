using System;

using Dapper;
namespace Mercadito
{
    public class ProductCategoryRepository : IProductCategoryRepository
    {
        private readonly IDataBaseConnection _dbConnection;
        private readonly string tableName = "categoriaDeProducto";
        private readonly ILogger<ProductCategoryRepository> _logger;
        public ProductCategoryRepository(IDataBaseConnection dbConnection, ILogger<ProductCategoryRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }
        public async Task<IEnumerable<ProductCategory>> GetAllProductCategoriesAsync()
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"SELECT BIN_TO_UUID(productId) AS ProductId, BIN_TO_UUID(categoriaId) AS CategoryId FROM {tableName}";
                return await connection.QueryAsync<ProductCategory>(query);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all product categories");
                throw;
            }
        }
        public async Task<ProductCategory?> GetProductsCategoriesByProductIdAsync(Guid productId)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"SELECT BIN_TO_UUID(productId) AS ProductId, BIN_TO_UUID(categoriaId) AS CategoryId FROM {tableName} WHERE productId = UUID_TO_BIN(@ProductId)";
                return await connection.QueryFirstOrDefaultAsync<ProductCategory>(query, new { ProductId = productId.ToString() });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product category by product ID");
                throw;
            }
        }
        public async Task<ProductCategory?> GetProductsCategoriesByCategoryIdAsync(Guid categoryId)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"SELECT BIN_TO_UUID(productId) AS ProductId, BIN_TO_UUID(categoriaId) AS CategoryId FROM {tableName} WHERE categoriaId = UUID_TO_BIN(@CategoryId)";
                return await connection.QueryFirstOrDefaultAsync<ProductCategory>(query, new { CategoryId = categoryId.ToString() });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product category by category ID");
                throw;
            }
        }
        public async Task AddProductCategoryAsync(ProductCategory productCategory)
        {
           try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"INSERT INTO {tableName} (productId, categoriaId) VALUES (UUID_TO_BIN(@ProductId), UUID_TO_BIN(@CategoryId))";
                await connection.ExecuteAsync(query, new { ProductId = productCategory.ProductId.ToString(), CategoryId = productCategory.CategoryId.ToString() });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error adding product category");
                throw;
            }
        }
        public async Task DeleteProductCategoryAsync(ProductCategory productCategory)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"DELETE FROM {tableName} WHERE productId = UUID_TO_BIN(@ProductId) AND categoriaId = UUID_TO_BIN(@CategoryId)";
                await connection.ExecuteAsync(query, new { ProductId = productCategory.ProductId.ToString(), CategoryId = productCategory.CategoryId.ToString() });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error deleting product category relation");
                throw;
            }
        }

        public async Task DeleteProductCategoriesByProductIdAsync(Guid productId)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"DELETE FROM {tableName} WHERE productId = UUID_TO_BIN(@ProductId)";
                await connection.ExecuteAsync(query, new { ProductId = productId.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product category relations by product ID");
                throw;
            }
        }
    }
}