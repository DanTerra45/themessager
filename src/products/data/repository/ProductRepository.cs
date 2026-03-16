using System.Data;
using System.Text;
using Dapper;
using Mercadito.database.interfaces;
using Mercadito.src.products.data.entity;
using Mercadito.src.products.domain.model;
using Mercadito.src.products.domain.repository;

namespace Mercadito.src.products.data.repository
{
    public class ProductRepository(IDataBaseConnection dbConnection) : IProductRepository
    {
        private const string ActiveState = "A";
        private const string InactiveState = "I";

        private readonly IDataBaseConnection _dbConnection = dbConnection;

        private static DateOnly ToDateOnly(DateTime value)
        {
            return DateOnly.FromDateTime(value);
        }

        private static DateTime ToDateTime(DateOnly value)
        {
            return value.ToDateTime(TimeOnly.MinValue);
        }

        private static ProductWithCategoriesModel ToProductWithCategoriesModel((
            long Id,
            string Name,
            string Description,
            int Stock,
            string Batch,
            DateTime ExpirationDate,
            decimal Price,
            string CategoriesString) row)
        {
            return new ProductWithCategoriesModel
            {
                Id = row.Id,
                Name = row.Name,
                Description = row.Description,
                Stock = row.Stock,
                Batch = row.Batch,
                ExpirationDate = ToDateOnly(row.ExpirationDate),
                Price = row.Price,
                Categories = !string.IsNullOrWhiteSpace(row.CategoriesString)
                    ? [.. row.CategoriesString.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)]
                    : []
            };
        }

        private static List<long> NormalizeCategoryIds(IReadOnlyList<long> categoryIds)
        {
            var normalizedCategoryIds = new List<long>();
            var uniqueCategoryIds = new HashSet<long>();

            foreach (var categoryId in categoryIds)
            {
                if (categoryId <= 0)
                {
                    continue;
                }

                if (uniqueCategoryIds.Add(categoryId))
                {
                    normalizedCategoryIds.Add(categoryId);
                }
            }

            return normalizedCategoryIds;
        }

        private static List<long> ParseCategoryIds(string categoryIdsString)
        {
            if (string.IsNullOrWhiteSpace(categoryIdsString))
            {
                return [];
            }

            var categoryIds = new List<long>();
            var uniqueCategoryIds = new HashSet<long>();

            foreach (var rawCategoryId in categoryIdsString.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (!long.TryParse(rawCategoryId, out var categoryId) || categoryId <= 0)
                {
                    continue;
                }

                if (uniqueCategoryIds.Add(categoryId))
                {
                    categoryIds.Add(categoryId);
                }
            }

            return categoryIds;
        }

        private static CommandDefinition BuildInsertProductCategoriesCommand(
            long productId,
            IReadOnlyList<long> normalizedCategoryIds,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            var queryBuilder = new StringBuilder("INSERT INTO categoriaDeProducto (productId, categoriaId) VALUES ");
            var parameters = new DynamicParameters();
            parameters.Add("ProductId", productId);

            for (var index = 0; index < normalizedCategoryIds.Count; index++)
            {
                if (index > 0)
                {
                    queryBuilder.Append(", ");
                }

                var categoryParameterName = $"CategoryId{index}";
                queryBuilder.Append("(@ProductId, @")
                    .Append(categoryParameterName)
                    .Append(')');

                parameters.Add(categoryParameterName, normalizedCategoryIds[index]);
            }

            return new CommandDefinition(
                queryBuilder.ToString(),
                parameters: parameters,
                transaction: transaction,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<ProductWithCategoriesModel>> GetProductsWithCategoriesByPages(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            var offset = (page - 1) * pageSize;

            const string query = @"
                SELECT 
                    p.id as Id,
                    p.nombre as Name,
                    p.descripcion as Description,
                    p.stock as Stock,
                    p.lote as Batch,
                    p.fechaCaducidad as ExpirationDate,
                    p.precio as Price,
                    COALESCE(GROUP_CONCAT(DISTINCT c.nombre SEPARATOR ','), '') as CategoriesString
                FROM products p
                LEFT JOIN categoriaDeProducto pc ON p.id = pc.productId
                LEFT JOIN categorias c ON pc.categoriaId = c.id AND c.estado = @ActiveState
                WHERE p.estado = @ActiveState
                GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio
                ORDER BY p.nombre
                LIMIT @PageSize OFFSET @Offset";

            var command = new CommandDefinition(
                query,
                parameters: new { ActiveState, Offset = offset, PageSize = pageSize },
                cancellationToken: cancellationToken);

            var products = (await connection.QueryAsync<(long Id, string Name, string Description, int Stock, string Batch, DateTime ExpirationDate, decimal Price, string CategoriesString)>(
                command)).AsList();

            return [.. products.Select(ToProductWithCategoriesModel)];
        }

        public async Task<IReadOnlyList<ProductWithCategoriesModel>> GetProductsWithCategoriesFilterByCategoryByPages(int page, long categoryId, int pageSize, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            var offset = (page - 1) * pageSize;
            const string query = @"
                SELECT 
                    p.id as Id,
                    p.nombre as Name,
                    p.descripcion as Description,
                    p.stock as Stock,
                    p.lote as Batch,
                    p.fechaCaducidad as ExpirationDate,
                    p.precio as Price,
                    COALESCE(GROUP_CONCAT(DISTINCT c.nombre SEPARATOR ','), '') as CategoriesString
                FROM products p
                INNER JOIN categoriaDeProducto pc ON p.id = pc.productId
                LEFT JOIN categorias c ON pc.categoriaId = c.id AND c.estado = @ActiveState
                WHERE pc.categoriaId = @CategoryId AND p.estado = @ActiveState
                GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio
                ORDER BY p.nombre
                LIMIT @PageSize OFFSET @Offset";

            var command = new CommandDefinition(
                query,
                parameters: new { ActiveState, CategoryId = categoryId, Offset = offset, PageSize = pageSize },
                cancellationToken: cancellationToken);

            var products = (await connection.QueryAsync<(long Id, string Name, string Description, int Stock, string Batch, DateTime ExpirationDate, decimal Price, string CategoriesString)>(
                command)).AsList();

            return [.. products.Select(ToProductWithCategoriesModel)];
        }

        public async Task<Product?> GetProductByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"SELECT 
                            id AS Id,
                            nombre AS Name,
                            descripcion AS Description,
                            stock AS Stock,
                            lote AS Batch,
                            fechaCaducidad AS ExpirationDate,
                            precio AS Price
                        FROM products 
                        WHERE id = @Id AND estado = @ActiveState";

            var command = new CommandDefinition(
                query,
                parameters: new { Id = id, ActiveState },
                cancellationToken: cancellationToken);

            var row = await connection.QueryFirstOrDefaultAsync<(long Id, string Name, string Description, int Stock, string Batch, DateTime ExpirationDate, decimal Price)>(command);
            if (row == default)
            {
                return null;
            }

            return new Product
            {
                Id = row.Id,
                Name = row.Name,
                Description = row.Description,
                Stock = row.Stock,
                Batch = row.Batch,
                ExpirationDate = ToDateOnly(row.ExpirationDate),
                Price = row.Price
            };
        }

        public async Task<ProductForEditModel?> GetProductForEditAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"SELECT 
                            p.id AS Id,
                            p.nombre AS Name,
                            p.descripcion AS Description,
                            p.stock AS Stock,
                            p.lote AS Batch,
                            p.fechaCaducidad AS ExpirationDate,
                            p.precio AS Price,
                            COALESCE(GROUP_CONCAT(DISTINCT cp.categoriaId ORDER BY cp.categoriaId SEPARATOR ','), '') AS CategoryIdsString
                        FROM products p
                        LEFT JOIN categoriaDeProducto cp ON p.id = cp.productId
                        WHERE p.id = @Id AND p.estado = @ActiveState
                        GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio";

            var command = new CommandDefinition(
                query,
                parameters: new { Id = id, ActiveState },
                cancellationToken: cancellationToken);

            var productForEditRow = await connection.QueryFirstOrDefaultAsync<(
                long Id,
                string Name,
                string Description,
                int Stock,
                string Batch,
                DateTime ExpirationDate,
                decimal Price,
                string CategoryIdsString)>(command);

            if (productForEditRow == default)
            {
                return null;
            }

            return new ProductForEditModel
            {
                Id = productForEditRow.Id,
                Name = productForEditRow.Name,
                Description = productForEditRow.Description,
                Stock = productForEditRow.Stock,
                Batch = productForEditRow.Batch,
                ExpirationDate = ToDateOnly(productForEditRow.ExpirationDate),
                Price = productForEditRow.Price,
                CategoryIds = ParseCategoryIds(productForEditRow.CategoryIdsString)
            };
        }

        public async Task<long> AddProductWithCategoriesAsync(Product product, IReadOnlyList<long> categoryIds, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                const string insertProductQuery = "INSERT INTO products (nombre, descripcion, stock, lote, fechaCaducidad, precio, estado) VALUES (@Name, @Description, @Stock, @Batch, @ExpirationDate, @Price, @ActiveState); SELECT LAST_INSERT_ID();";
                var insertProductCommand = new CommandDefinition(
                    insertProductQuery,
                    parameters: new
                    {
                        product.Name,
                        product.Description,
                        product.Stock,
                        Batch = product.Batch,
                        ExpirationDate = ToDateTime(product.ExpirationDate),
                        product.Price,
                        ActiveState
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var createdProductId = await connection.ExecuteScalarAsync<long>(insertProductCommand);

                var normalizedCategoryIds = NormalizeCategoryIds(categoryIds);
                if (normalizedCategoryIds.Count > 0)
                {
                    var insertCategoriesCommand = BuildInsertProductCategoriesCommand(
                        createdProductId,
                        normalizedCategoryIds,
                        transaction,
                        cancellationToken);

                    await connection.ExecuteAsync(insertCategoriesCommand);
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

        public async Task<int> UpdateProductWithCategoriesAsync(Product product, IReadOnlyList<long> categoryIds, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                const string updateProductQuery = @"UPDATE products
                    SET nombre = @Name,
                        descripcion = @Description,
                        stock = @Stock,
                        lote = @Batch,
                        fechaCaducidad = @ExpirationDate,
                        precio = @Price
                    WHERE id = @Id AND estado = @ActiveState";

                var updateProductCommand = new CommandDefinition(
                    updateProductQuery,
                    parameters: new
                    {
                        product.Id,
                        product.Name,
                        product.Description,
                        product.Stock,
                        Batch = product.Batch,
                        ExpirationDate = ToDateTime(product.ExpirationDate),
                        product.Price,
                        ActiveState
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var affectedRows = await connection.ExecuteAsync(updateProductCommand);

                if (affectedRows == 0)
                {
                    transaction.Rollback();
                    return 0;
                }

                const string deleteRelationsQuery = @"DELETE FROM categoriaDeProducto
                    WHERE productId = @ProductId";

                var deleteRelationsCommand = new CommandDefinition(
                    deleteRelationsQuery,
                    parameters: new { ProductId = product.Id },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(deleteRelationsCommand);

                var normalizedCategoryIds = NormalizeCategoryIds(categoryIds);
                if (normalizedCategoryIds.Count > 0)
                {
                    var insertCategoriesCommand = BuildInsertProductCategoriesCommand(
                        product.Id,
                        normalizedCategoryIds,
                        transaction,
                        cancellationToken);

                    await connection.ExecuteAsync(insertCategoriesCommand);
                }

                transaction.Commit();
                return affectedRows;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> DeleteProductAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"UPDATE products
                SET estado = @InactiveState
                WHERE id = @Id AND estado = @ActiveState";

            var command = new CommandDefinition(
                query,
                parameters: new { Id = id, ActiveState, InactiveState },
                cancellationToken: cancellationToken);

            return await connection.ExecuteAsync(command);
        }

        public async Task<int> GetTotalProductsCountAsync(CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = "SELECT COUNT(*) FROM products WHERE estado = @ActiveState";

            var command = new CommandDefinition(query, parameters: new { ActiveState }, cancellationToken: cancellationToken);
            return await connection.ExecuteScalarAsync<int>(command);
        }

        public async Task<int> GetTotalProductsCountByCategoryAsync(long categoryId, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"SELECT COUNT(DISTINCT p.id)
                     FROM products p
                     INNER JOIN categoriaDeProducto pc ON p.id = pc.productId
                    WHERE pc.categoriaId = @CategoryId AND p.estado = @ActiveState";

            var command = new CommandDefinition(
                query,
                parameters: new { CategoryId = categoryId, ActiveState },
                cancellationToken: cancellationToken);

            return await connection.ExecuteScalarAsync<int>(command);
        }
    }
}
