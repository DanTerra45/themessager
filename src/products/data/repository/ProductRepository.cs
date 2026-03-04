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
                var query = $"SELECT BIN_TO_UUID(id) AS Id, nombre AS Name, descripcion AS Description, stock AS Stock, lote AS Lote, fechaCaducidad AS FechaDeCaducidad, precio AS Price FROM {tableName}";
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
                var query = $@"SELECT BIN_TO_UUID(p.id) AS Id, p.nombre AS Name, p.descripcion AS Description, p.stock AS Stock, p.lote AS Lote, p.fechaCaducidad AS FechaDeCaducidad, p.precio AS Price, c.nombre AS Category
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
                // Cambiado a sintaxis MySQL
                var query = $"SELECT BIN_TO_UUID(id) AS Id, nombre AS Name, descripcion AS Description, stock AS Stock, lote AS Lote, fechaCaducidad AS FechaDeCaducidad, precio AS Price FROM {tableName} ORDER BY id LIMIT @PageSize OFFSET @Offset";
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
                using var connection = await _dbConnection.CreateConnectionAsync();
                
                var offset = (page - 1) * 10;
                
                var query = $@"
                    SELECT 
                        BIN_TO_UUID(p.id) as Id,
                        p.nombre as Name,
                        p.descripcion as Description,
                        p.stock as Stock,
                        p.lote as Lote,
                        p.fechaCaducidad as FechaDeCaducidad,
                        p.precio as Price,
                        GROUP_CONCAT(DISTINCT c.nombre SEPARATOR ',') as CategoriesString
                    FROM {tableName} p
                    LEFT JOIN {relationTableName} pc ON p.id = pc.productId
                    LEFT JOIN categorias c ON pc.categoriaId = c.id
                    GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio
                    ORDER BY p.nombre
                    LIMIT 10 OFFSET @Offset";
                
                var products = await connection.QueryAsync<dynamic>(query, new { Offset = offset });
                var productsList = products.Select(p => new ProductWithCategoriesModel
                {
                    Id = Guid.Parse((string)p.Id),
                    Name = (string)p.Name,
                    Description = (string)p.Description,
                    Stock = (int)p.Stock,
                    Lote = (DateTime)p.Lote,
                    FechaDeCaducidad = (DateTime)p.FechaDeCaducidad,
                    Price = (decimal)(double)p.Price,
                    Categories = !string.IsNullOrEmpty((string)p.CategoriesString) 
                        ? ((string)p.CategoriesString).Split(',').ToList() 
                        : new List<string>()
                }).ToList();
                
                return productsList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos paginados");
                throw;
            }
        }
        public async Task<IEnumerable<ProductWithCategoriesModel>> GetProductsWithCategoriesFilterByCategoryByPages(int page, Guid categoryId)
        {            
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                
                var offset = (page - 1) * 10;
                var query = $@"
                    SELECT 
                        BIN_TO_UUID(p.id) as Id,
                        p.nombre as Name,
                        p.descripcion as Description,
                        p.stock as Stock,
                        p.lote as Lote,
                        p.fechaCaducidad as FechaDeCaducidad,
                        p.precio as Price,
                        GROUP_CONCAT(DISTINCT c.nombre SEPARATOR ',') as CategoriesString
                    FROM {tableName} p
                    INNER JOIN {relationTableName} pc ON p.id = pc.productId
                    LEFT JOIN categorias c ON pc.categoriaId = c.id
                    WHERE pc.categoriaId = UUID_TO_BIN(@CategoryId)
                    GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio
                    ORDER BY p.nombre
                    LIMIT 10 OFFSET @Offset";
                var products = await connection.QueryAsync<dynamic>(query, new { CategoryId = categoryId.ToString(), Offset = offset });
                var productsList = products.Select(p => new ProductWithCategoriesModel
                {
                    Id = Guid.Parse((string)p.Id),
                    Name = (string)p.Name,
                    Description = (string)p.Description,
                    Stock = (int)p.Stock,
                    Lote = (DateTime)p.Lote,
                    FechaDeCaducidad = (DateTime)p.FechaDeCaducidad,
                    Price = (decimal)(double)p.Price,
                    Categories = !string.IsNullOrEmpty((string)p.CategoriesString) 
                        ? ((string)p.CategoriesString).Split(',').ToList() 
                        : new List<string>()
                }).ToList();
                
                return productsList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos paginados con filtro");
                throw;
            }
        }
        public async Task<Product> GetProductByIdAsync(Guid id)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"SELECT BIN_TO_UUID(id) AS Id, nombre AS Name, descripcion AS Description, stock AS Stock, lote AS Lote, fechaCaducidad AS FechaDeCaducidad, precio AS Price FROM {tableName} WHERE id = UUID_TO_BIN(@Id)";
                return await connection.QueryFirstOrDefaultAsync<Product>(query, new { Id = id.ToString() });
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
                var query = $"INSERT INTO {tableName} (id,nombre, descripcion, stock, lote, fechaCaducidad, precio) VALUES (UUID_TO_BIN(@Id), @Name, @Description, @Stock, @Lote, @FechaDeCaducidad, @Price)";
                var result = await connection.ExecuteAsync(query, new {
                    Id = productId.ToString(),
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

        public async Task UpdateProductAsync(Product product)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"UPDATE {tableName} SET nombre = @Name, descripcion = @Description, stock = @Stock, lote = @Lote, fechaCaducidad = @FechaDeCaducidad, precio = @Price WHERE id = UUID_TO_BIN(@Id)";
                await connection.ExecuteAsync(query, new
                {
                    Id = product.Id.ToString(),
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
                var query = $"DELETE FROM {tableName} WHERE id = UUID_TO_BIN(@Id)";
                await connection.ExecuteAsync(query, new { Id = id.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el producto");
                throw;
            }
        }
        public async Task<int> GetTotalProductsCountAsync()
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"SELECT COUNT(*) FROM {tableName}";
                return await connection.ExecuteScalarAsync<int>(query);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el conteo total de productos");
                throw;
            }  
        }
        public async Task<int> GetTotalProductsCountByCategoryAsync(Guid categoryId)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $@"SELECT COUNT(DISTINCT p.id) 
                       FROM {tableName} p
                       INNER JOIN {relationTableName} pc ON p.id = pc.productId
                       WHERE pc.categoriaId = UUID_TO_BIN(@CategoryId)";
                return await connection.ExecuteScalarAsync<int>(query, new { CategoryId = categoryId.ToString() });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el conteo de productos por categoría: {categoryId}");
                throw;
            }
        }
    }
}