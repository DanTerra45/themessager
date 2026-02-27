using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace Mercadito
{
    public class ProductRepository : IProductRepository
    {
        private readonly IDataBaseConnection _dbConnection;
        private readonly string tableName = "products";
        private readonly string relationTableName = "categoriaDeProducto";
        private readonly ILogger<ProductRepository> _logger;
        public ProductRepository(IDataBaseConnection dbConnection, ILogger<ProductRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }
        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"SELECT id AS Id, nombre AS Name, descripcion AS Description, stock AS Stock, lote AS Lote, fechaCaducidad AS FechaDeCaducidad, precio AS Price FROM {tableName}";
                return await connection.QueryAsync<Product>(query);
                
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los productos");
                throw;
            }
        }
        public async Task<IEnumerable<ProductWithCategoriesModel>> GetAllProductsWithCategoriesAsync()
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $@"SELECT p.id AS Id, p.nombre AS Name, p.descripcion AS Description, p.stock AS Stock, p.lote AS Lote, p.fechaCaducidad AS FechaDeCaducidad, p.precio AS Price, c.nombre AS Category
                               FROM {tableName} p
                               LEFT JOIN {relationTableName} pc ON p.id = pc.productId
                               LEFT JOIN categorias c ON pc.categoriaId = c.id";
                var productDictionary = new Dictionary<Guid, ProductWithCategoriesModel>();

                var products = await connection.QueryAsync<ProductWithCategoriesModel, string, ProductWithCategoriesModel>(
                    query,
                    (product, category) =>
                    {
                        if (!productDictionary.TryGetValue(product.Id, out var productEntry))
                        {
                            productEntry = product;
                            productEntry.Categories = new List<string>();
                            productDictionary.Add(productEntry.Id, productEntry);
                        }
                        if (!string.IsNullOrEmpty(category) && !productEntry.Categories.Contains(category))
                        {
                            productEntry.Categories.Add(category);
                        }
                        return productEntry;
                    },
                    splitOn: "Category"
                );

                return products.Distinct().ToList();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los productos con categorías");
                throw;
            }
        }
        public async Task<IEnumerable<Product>> GetProductsByPages(int page)
        {
            try
            {
                int pageSize = 10;
                int offset = (page - 1) * pageSize;
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"SELECT id AS Id, nombre AS Name, descripcion AS Description, stock AS Stock, lote AS Lote, fechaCaducidad AS FechaDeCaducidad, precio AS Price FROM {tableName} ORDER BY id OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                return await connection.QueryAsync<Product>(query, new { Offset = offset, PageSize = pageSize });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener productos por página: {page}");
                throw;
            }
        }
        public async Task<IEnumerable<ProductWithCategoriesModel>> GetProductsWithCategoriesByPages(int page)
        {
            try
            {
                int pageSize = 10;
                int offset = (page - 1) * pageSize;
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $@"SELECT p.id AS Id, p.nombre AS Name, p.descripcion AS Description, p.stock AS Stock, p.lote AS Lote, p.fechaCaducidad AS FechaDeCaducidad, p.precio AS Price, c.nombre AS Category
                               FROM {tableName} p
                               LEFT JOIN {relationTableName} pc ON p.id = pc.productId
                               LEFT JOIN categorias c ON pc.categoriaId = c.id
                               ORDER BY p.id OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                var productDictionary = new Dictionary<Guid, ProductWithCategoriesModel>();

                var products = await connection.QueryAsync<ProductWithCategoriesModel, string, ProductWithCategoriesModel>(
                    query,
                    (product, category) =>
                    {
                        if (!productDictionary.TryGetValue(product.Id, out var productEntry))
                        {
                            productEntry = product;
                            productEntry.Categories = new List<string>();
                            productDictionary.Add(productEntry.Id, productEntry);
                        }
                        if (!string.IsNullOrEmpty(category) && !productEntry.Categories.Contains(category))
                        {
                            productEntry.Categories.Add(category);
                        }
                        return productEntry;
                    },
                    new { Offset = offset, PageSize = pageSize },
                    splitOn: "Category"
                );

                return products.Distinct().ToList();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener productos con categorías por página: {page}");
                throw;
            }
        }
        public async Task<Product> GetProductByIdAsync(Guid id)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"SELECT id AS Id, nombre AS Name, descripcion AS Description, stock AS Stock, lote AS Lote, fechaCaducidad AS FechaDeCaducidad, precio AS Price FROM {tableName} WHERE id = @Id";
                return await connection.QueryFirstOrDefaultAsync<Product>(query, new { Id = id });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el producto con ID: {id}");
                throw;
            }
        }
        public async Task<Guid> AddProductAsync(CreateProductDto product)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var productId = Guid.NewGuid();
                var query = $"INSERT INTO {tableName} (id,nombre, descripcion, stock, lote, fechaCaducidad, precio) VALUES (@Id, @Name, @Description, @Stock, @Lote, @FechaDeCaducidad, @Price)";
                var result = await connection.ExecuteAsync(query, new {
                    Id = productId,
                    Name = product.Name,
                    Description = product.Description,
                    Stock = product.Stock,
                    Lote = product.Lote,
                    FechaDeCaducidad = product.FechaDeCaducidad,
                    Price = product.Price
                });
                return result > 0 ? productId : Guid.Empty;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al agregar un producto");
                throw;
            }
        }
        public async Task<IEnumerable<ProductWithCategoriesModel>> GetProductsWithCategoriesFilterByCategoryByPages(int page, Guid categoryId)
        {
            try
            {
                int pageSize = 10;
                int offset = (page - 1) * pageSize;
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $@"SELECT p.id AS Id, p.nombre AS Name, p.descripcion AS Description, p.stock AS Stock, p.lote AS Lote, p.fechaCaducidad AS FechaDeCaducidad, p.precio AS Price, c.nombre AS Category
                               FROM {tableName} p
                               INNER JOIN {relationTableName} pc ON p.id = pc.productId
                               INNER JOIN categorias c ON pc.categoriaId = c.id
                               WHERE c.id = @CategoryId
                               ORDER BY p.id OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                var productDictionary = new Dictionary<Guid, ProductWithCategoriesModel>();

                var products = await connection.QueryAsync<ProductWithCategoriesModel, string, ProductWithCategoriesModel>(
                    query,
                    (product, category) =>
                    {
                        if (!productDictionary.TryGetValue(product.Id, out var productEntry))
                        {
                            productEntry = product;
                            productEntry.Categories = new List<string>();
                            productDictionary.Add(productEntry.Id, productEntry);
                        }
                        if (!string.IsNullOrEmpty(category) && !productEntry.Categories.Contains(category))
                        {
                            productEntry.Categories.Add(category);
                        }
                        return productEntry;
                    },
                    new { CategoryId = categoryId, Offset = offset, PageSize = pageSize },
                    splitOn: "Category"
                );

                return products.Distinct().ToList();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener productos con categorías filtrados por categoría por página: {page}");
                throw;
            }
        }
        public async Task UpdateProductAsync(Product product)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"UPDATE {tableName} SET nombre = @Name, descripcion = @Description, stock = @Stock, lote = @Lote, fechaCaducidad = @FechaDeCaducidad, precio = @Price WHERE id = @Id";
                await connection.ExecuteAsync(query, new
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Stock = product.Stock,
                    Lote = product.Lote,
                    FechaDeCaducidad = product.FechaDeCaducidad,
                    Price = product.Price
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el producto");
                throw;
            }
        }
        public async Task DeleteProductAsync(Guid id)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"DELETE FROM {tableName} WHERE id = @Id";
                await connection.ExecuteAsync(query, new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el producto");
                throw;
            }
        }
    }
}