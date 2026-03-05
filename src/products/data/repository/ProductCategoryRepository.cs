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
                var query = $"SELECT productId AS ProductId, categoriaId AS CategoryId FROM {tableName}";
                return await connection.QueryAsync<ProductCategory>(query);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all product categories");
                throw;
            }
        }
        public async Task<ProductCategory?> GetProductsCategoriesByProductIdAsync(long productId)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"SELECT productId AS ProductId, categoriaId AS CategoryId FROM {tableName} WHERE productId = @ProductId";
                return await connection.QueryFirstOrDefaultAsync<ProductCategory>(query, new { ProductId = productId });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product category by product ID");
                throw;
            }
        }
        public async Task<ProductCategory?> GetProductsCategoriesByCategoryIdAsync(long categoryId)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"SELECT productId AS ProductId, categoriaId AS CategoryId FROM {tableName} WHERE categoriaId = @CategoryId";
                return await connection.QueryFirstOrDefaultAsync<ProductCategory>(query, new { CategoryId = categoryId });
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
                var query = $"INSERT INTO {tableName} (productId, categoriaId) VALUES (@ProductId, @CategoryId)";
                await connection.ExecuteAsync(query, new { ProductId = productCategory.ProductId, CategoryId = productCategory.CategoryId });
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
                var query = $"DELETE FROM {tableName} WHERE productId = @ProductId AND categoriaId = @CategoryId";
                await connection.ExecuteAsync(query, new { ProductId = productCategory.ProductId, CategoryId = productCategory.CategoryId });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error deleting product category relation");
                throw;
            }
        }

        public async Task DeleteProductCategoriesByProductIdAsync(long productId)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"DELETE FROM {tableName} WHERE productId = @ProductId";
                await connection.ExecuteAsync(query, new { ProductId = productId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product category relations by product ID");
                throw;
            }
        }
    }
}