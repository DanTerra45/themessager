using Dapper;
using Mercadito.database.interfaces;
using Mercadito.src.products.data.entity;
using Mercadito.src.products.domain.model;
using Mercadito.src.products.domain.repository;

namespace Mercadito.src.products.data.repository
{
    public class ProductRepository(IDataBaseConnection dbConnection) : IProductRepository
    {
        private sealed class ProductWithCategoriesRow
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int Stock { get; set; }
            public DateTime Batch { get; set; }
            public DateTime ExpirationDate { get; set; }
            public decimal Price { get; set; }
            public string CategoriesString { get; set; } = string.Empty;
        }

        private readonly IDataBaseConnection _dbConnection = dbConnection;
        private readonly string tableName = "products";
        private readonly string relationTableName = "categoriaDeProducto";

        private static ProductWithCategoriesModel ToProductWithCategoriesModel(ProductWithCategoriesRow row)
        {
            return new ProductWithCategoriesModel
            {
                Id = row.Id,
                Name = row.Name,
                Description = row.Description,
                Stock = row.Stock,
                Batch = row.Batch,
                ExpirationDate = row.ExpirationDate,
                Price = row.Price,
                Categories = !string.IsNullOrEmpty(row.CategoriesString)
                    ? [.. row.CategoriesString.Split(',')]
                    : []
            };
        }

        public async Task<IEnumerable<ProductWithCategoriesModel>> GetProductsWithCategoriesByPages(int page, int pageSize)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            var offset = (page - 1) * pageSize;

            var query = $@"
                SELECT 
                    p.id as Id,
                    COALESCE(p.nombre, '') as Name,
                    COALESCE(p.descripcion, '') as Description,
                    COALESCE(p.stock, 0) as Stock,
                    p.lote as Batch,
                    p.fechaCaducidad as ExpirationDate,
                    COALESCE(p.precio, 0.00) as Price,
                    COALESCE(GROUP_CONCAT(DISTINCT c.nombre SEPARATOR ','), '') as CategoriesString
                FROM {tableName} p
                LEFT JOIN {relationTableName} pc ON p.id = pc.productId
                LEFT JOIN categorias c ON pc.categoriaId = c.id AND c.estado = 'A'
                WHERE p.estado = 'A'
                GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio
                ORDER BY p.nombre
                LIMIT @PageSize OFFSET @Offset";

            var products = await connection.QueryAsync<ProductWithCategoriesRow>(query, new { Offset = offset, PageSize = pageSize });
            return [.. products.Select(ToProductWithCategoriesModel)];
        }

        public async Task<IEnumerable<ProductWithCategoriesModel>> GetProductsWithCategoriesFilterByCategoryByPages(int page, long categoryId, int pageSize)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            var offset = (page - 1) * pageSize;
            var query = $@"
                SELECT 
                    p.id as Id,
                    COALESCE(p.nombre, '') as Name,
                    COALESCE(p.descripcion, '') as Description,
                    COALESCE(p.stock, 0) as Stock,
                    p.lote as Batch,
                    p.fechaCaducidad as ExpirationDate,
                    COALESCE(p.precio, 0.00) as Price,
                    COALESCE(GROUP_CONCAT(DISTINCT c.nombre SEPARATOR ','), '') as CategoriesString
                FROM {tableName} p
                INNER JOIN {relationTableName} pc ON p.id = pc.productId
                LEFT JOIN categorias c ON pc.categoriaId = c.id AND c.estado = 'A'
                WHERE pc.categoriaId = @CategoryId AND p.estado = 'A'
                GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio
                ORDER BY p.nombre
                LIMIT @PageSize OFFSET @Offset";
            var products = await connection.QueryAsync<ProductWithCategoriesRow>(query, new { CategoryId = categoryId, Offset = offset, PageSize = pageSize });
            return [.. products.Select(ToProductWithCategoriesModel)];
        }

        public async Task<Product?> GetProductByIdAsync(long id)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            var query = $@"SELECT 
                            id AS Id,
                            COALESCE(nombre, '') AS Name,
                            COALESCE(descripcion, '') AS Description,
                            COALESCE(stock, 0) AS Stock,
                            lote AS Batch,
                            fechaCaducidad AS ExpirationDate,
                            COALESCE(precio, 0.00) AS Price
                          FROM {tableName} 
                          WHERE id = @Id AND estado = 'A'";
            return await connection.QueryFirstOrDefaultAsync<Product>(query, new { Id = id });
        }

        public async Task<ProductForEditModel?> GetProductForEditAsync(long id)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            var query = $@"SELECT 
                            p.id AS Id,
                            COALESCE(p.nombre, '') AS Name,
                            COALESCE(p.descripcion, '') AS Description,
                            COALESCE(p.stock, 0) AS Stock,
                            p.lote AS Batch,
                            p.fechaCaducidad AS ExpirationDate,
                            COALESCE(p.precio, 0.00) AS Price,
                            COALESCE(cp.categoriaId, 0) AS CategoryId
                          FROM {tableName} p
                          LEFT JOIN {relationTableName} cp ON p.id = cp.productId
                          WHERE p.id = @Id AND p.estado = 'A'
                          LIMIT 1";
            return await connection.QueryFirstOrDefaultAsync<ProductForEditModel>(query, new { Id = id });
        }

        public async Task<long> AddProductWithCategoryAsync(Product product, long categoryId)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var insertProductQuery = $"INSERT INTO {tableName} (nombre, descripcion, stock, lote, fechaCaducidad, precio, estado) VALUES (@Name, @Description, @Stock, @Batch, @ExpirationDate, @Price, 'A'); SELECT LAST_INSERT_ID();";
                var createdProductId = await connection.ExecuteScalarAsync<long>(insertProductQuery, new
                {
                    product.Name,
                    product.Description,
                    product.Stock,
                    product.Batch,
                    product.ExpirationDate,
                    product.Price
                }, transaction);

                if (categoryId > 0)
                {
                    var insertRelationQuery = $@"INSERT INTO {relationTableName} (productId, categoriaId)
                                             VALUES (@ProductId, @CategoryId)";
                    await connection.ExecuteAsync(insertRelationQuery, new { ProductId = createdProductId, CategoryId = categoryId }, transaction);
                }

                transaction.Commit();
                return createdProductId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task UpdateProductWithCategoryAsync(Product product, long categoryId)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var updateProductQuery = $@"UPDATE {tableName}
                                            SET nombre = @Name,
                                                descripcion = @Description,
                                                stock = @Stock,
                                                lote = @Batch,
                                                fechaCaducidad = @ExpirationDate,
                                                precio = @Price
                                            WHERE id = @Id
                                            AND estado = 'A'";

                var affectedRows = await connection.ExecuteAsync(updateProductQuery, new
                {
                    product.Id,
                    product.Name,
                    product.Description,
                    product.Stock,
                    product.Batch,
                    product.ExpirationDate,
                    product.Price
                }, transaction);

                if (affectedRows == 0)
                {
                    throw new InvalidOperationException("No se pudo actualizar el producto solicitado.");
                }

                var deleteRelationQuery = $@"DELETE FROM {relationTableName}
                                             WHERE productId = @ProductId";
                await connection.ExecuteAsync(deleteRelationQuery, new { ProductId = product.Id }, transaction);

                if (categoryId > 0)
                {
                    var insertRelationQuery = $@"INSERT INTO {relationTableName}
                                                 (productId, categoriaId)
                                                 VALUES (@ProductId, @CategoryId)";
                    await connection.ExecuteAsync(insertRelationQuery, new { ProductId = product.Id, CategoryId = categoryId }, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> DeleteProductAsync(long id)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            var query = $"UPDATE {tableName} SET estado = 'I' WHERE id = @Id AND estado = 'A'";
            return await connection.ExecuteAsync(query, new { Id = id });
        }

        public async Task<int> GetTotalProductsCountAsync()
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            var query = $"SELECT COUNT(*) FROM {tableName} WHERE estado = 'A'";
            return await connection.ExecuteScalarAsync<int>(query);
        }

        public async Task<int> GetTotalProductsCountByCategoryAsync(long categoryId)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            var query = $@"SELECT COUNT(DISTINCT p.id) 
                   FROM {tableName} p
                   INNER JOIN {relationTableName} pc ON p.id = pc.productId
                    WHERE pc.categoriaId = @CategoryId AND p.estado = 'A'";
            return await connection.ExecuteScalarAsync<int>(query, new { CategoryId = categoryId });
        }
    }
}
