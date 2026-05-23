namespace MSProducto.Infrastructure.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper;
    using MSProducto.Domain.Entities;
    using MSProducto.Domain.Ports.Output;

    public class MySqlProductoRepository : IProductoRepository
    {
        private const int ActiveState = 1;
        private const int InactiveState = 0;
        private readonly IDbConnectionFactory _factory;

        public MySqlProductoRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<Producto>> GetProductsWithCategoriesByCursorAsync(
            int pageSize, string sortBy, string sortDirection, long cursorProductId, bool isNextPage,
            string searchTerm = "", CancellationToken cancellationToken = default)
        {
            using var connection = _factory.CreateConnection();

            if (cursorProductId <= 0)
            {
                return new List<Producto>();
            }

            var normalizedSearchTerm = (searchTerm ?? string.Empty).Trim();
            var hasSearch = normalizedSearchTerm.Length > 0;
            var orderByClause = BuildOrderByClause(sortBy, sortDirection, isNextPage);
            var keysetComparator = BuildKeysetComparator(sortBy, sortDirection, isNextPage);

            var parameters = new DynamicParameters();
            parameters.Add("ActiveState", ActiveState);
            parameters.Add("CursorProductId", cursorProductId);
            parameters.Add("PageSize", pageSize);

            if (hasSearch)
            {
                parameters.Add("SearchPattern", "%" + normalizedSearchTerm + "%");
            }

            var queryBuilder = new StringBuilder(@"
                SELECT p.id
                FROM productos p
                INNER JOIN productos cursorProduct ON cursorProduct.id = @CursorProductId
                WHERE p.estado = @ActiveState
                AND cursorProduct.estado = @ActiveState");

            if (hasSearch)
            {
                queryBuilder.Append(@"
                AND (p.nombre LIKE @SearchPattern ESCAPE '\\'
                OR p.descripcion LIKE @SearchPattern ESCAPE '\\')");
            }

            queryBuilder.Append(@"
                AND ").Append(keysetComparator)
                .Append($@"
                ORDER BY {orderByClause}
                LIMIT @PageSize");

            var productIds = (await connection.QueryAsync<long>(new CommandDefinition(
                queryBuilder.ToString(), parameters, cancellationToken: cancellationToken))).AsList();

            if (productIds.Count == 0)
            {
                return new List<Producto>();
            }

            if (!isNextPage)
            {
                productIds.Reverse();
            }

            return await GetProductsByIdsAsync(connection, productIds, cancellationToken);
        }

        public async Task<IReadOnlyList<Producto>> GetProductsWithCategoriesByCategoryCursorAsync(
            long categoryId, int pageSize, string sortBy, string sortDirection, long cursorProductId,
            bool isNextPage, string searchTerm = "", CancellationToken cancellationToken = default)
        {
            using var connection = _factory.CreateConnection();

            if (cursorProductId <= 0)
            {
                return new List<Producto>();
            }

            var normalizedSearchTerm = (searchTerm ?? string.Empty).Trim();
            var hasSearch = normalizedSearchTerm.Length > 0;
            var orderByClause = BuildOrderByClause(sortBy, sortDirection, isNextPage);
            var keysetComparator = BuildKeysetComparator(sortBy, sortDirection, isNextPage);

            var parameters = new DynamicParameters();
            parameters.Add("ActiveState", ActiveState);
            parameters.Add("CursorProductId", cursorProductId);
            parameters.Add("CategoryId", categoryId);
            parameters.Add("PageSize", pageSize);

            if (hasSearch)
            {
                parameters.Add("SearchPattern", "%" + normalizedSearchTerm + "%");
            }

            var queryBuilder = new StringBuilder(@"
                SELECT p.id
                FROM productos p
                INNER JOIN productos cursorProduct ON cursorProduct.id = @CursorProductId
                INNER JOIN categoriaDeProducto pc ON p.id = pc.productId
                WHERE p.estado = @ActiveState
                AND cursorProduct.estado = @ActiveState
                AND pc.categoriaId = @CategoryId");

            if (hasSearch)
            {
                queryBuilder.Append(@"
                AND (p.nombre LIKE @SearchPattern ESCAPE '\\'
                OR p.descripcion LIKE @SearchPattern ESCAPE '\\')");
            }

            queryBuilder.Append(@"
                AND ").Append(keysetComparator)
                .Append($@"
                ORDER BY {orderByClause}
                LIMIT @PageSize");

            var productIds = (await connection.QueryAsync<long>(new CommandDefinition(
                queryBuilder.ToString(), parameters, cancellationToken: cancellationToken))).AsList();

            if (productIds.Count == 0)
            {
                return new List<Producto>();
            }

            if (!isNextPage)
            {
                productIds.Reverse();
            }

            return await GetProductsByIdsAsync(connection, productIds, cancellationToken);
        }

        public async Task<IReadOnlyList<Producto>> GetProductsWithCategoriesFromAnchorAsync(
            long categoryId, int pageSize, string sortBy, string sortDirection, long anchorProductId,
            string searchTerm = "", CancellationToken cancellationToken = default)
        {
            using var connection = _factory.CreateConnection();

            var hasAnchor = anchorProductId > 0;
            var filterByCategory = categoryId > 0;
            var orderByClause = BuildOrderByClause(sortBy, sortDirection);

            var normalizedSearchTerm = (searchTerm ?? string.Empty).Trim();
            var hasSearch = normalizedSearchTerm.Length > 0;

            var parameters = new DynamicParameters();
            parameters.Add("ActiveState", ActiveState);
            parameters.Add("PageSize", pageSize);

            if (hasSearch)
            {
                parameters.Add("SearchPattern", "%" + normalizedSearchTerm + "%");
            }

            if (hasAnchor)
            {
                parameters.Add("AnchorProductId", anchorProductId);
            }

            if (filterByCategory)
            {
                parameters.Add("CategoryId", categoryId);
            }

            var queryBuilder = new StringBuilder(@"
                SELECT p.id
                FROM productos p");

            if (hasAnchor)
            {
                queryBuilder.Append(@"
                INNER JOIN productos anchor ON anchor.id = @AnchorProductId");
            }

            if (filterByCategory)
            {
                queryBuilder.Append(@"
                INNER JOIN categoriaDeProducto pc ON p.id = pc.productId");
            }

            queryBuilder.Append(@"
                WHERE p.estado = @ActiveState");

            if (hasAnchor)
            {
                queryBuilder.Append(@"
                AND anchor.estado = @ActiveState
                AND ").Append(BuildAnchorInclusiveComparator(sortBy, sortDirection));
            }

            if (hasSearch)
            {
                queryBuilder.Append(@"
                AND (p.nombre LIKE @SearchPattern ESCAPE '\\'
                OR p.descripcion LIKE @SearchPattern ESCAPE '\\')");
            }

            queryBuilder.Append($@"
                ORDER BY {orderByClause}
                LIMIT @PageSize");

            var productIds = (await connection.QueryAsync<long>(new CommandDefinition(
                queryBuilder.ToString(), parameters, cancellationToken: cancellationToken))).ToList();

            if (productIds.Count == 0)
            {
                return new List<Producto>();
            }

            return await GetProductsByIdsAsync(connection, productIds, cancellationToken);
        }

        public async Task<bool> HasProductsByCursorAsync(
            long categoryId, string sortBy, string sortDirection, long cursorProductId, bool isNextPage,
            string searchTerm = "", CancellationToken cancellationToken = default)
        {
            if (cursorProductId <= 0)
            {
                return false;
            }

            using var connection = _factory.CreateConnection();

            var filterByCategory = categoryId > 0;
            var normalizedSearchTerm = (searchTerm ?? string.Empty).Trim();
            var hasSearch = normalizedSearchTerm.Length > 0;
            var keysetComparator = BuildKeysetComparator(sortBy, sortDirection, isNextPage);
            var orderByClause = BuildOrderByClause(sortBy, sortDirection, isNextPage);

            var parameters = new DynamicParameters();
            parameters.Add("ActiveState", ActiveState);
            parameters.Add("CursorProductId", cursorProductId);

            if (filterByCategory)
            {
                parameters.Add("CategoryId", categoryId);
            }

            if (hasSearch)
            {
                parameters.Add("SearchPattern", "%" + normalizedSearchTerm + "%");
            }

            var queryBuilder = new StringBuilder(@"
                SELECT p.id
                FROM productos p
                INNER JOIN productos cursorProduct ON cursorProduct.id = @CursorProductId
                WHERE p.estado = @ActiveState
                AND cursorProduct.estado = @ActiveState");

            if (filterByCategory)
            {
                queryBuilder.Append(@"
                INNER JOIN categoriaDeProducto pc ON p.id = pc.productId");
            }

            if (hasSearch)
            {
                queryBuilder.Append(@"
                AND (p.nombre LIKE @SearchPattern ESCAPE '\\'
                OR p.descripcion LIKE @SearchPattern ESCAPE '\\')");
            }

            queryBuilder.Append(@"
                AND ").Append(keysetComparator)
                .Append($@"
                ORDER BY {orderByClause}
                LIMIT 1");

            var nextProductId = await connection.QueryFirstOrDefaultAsync<long?>(new CommandDefinition(
                queryBuilder.ToString(), parameters, cancellationToken: cancellationToken));
            return nextProductId.HasValue;
        }

        public async Task<Producto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = _factory.CreateConnection();
            const string query = @"SELECT 
                p.id AS Id,
                p.nombre AS Name,
                p.descripcion AS Description,
                p.stock AS Stock,
                p.lote AS Batch,
                p.fechaCaducidad AS ExpirationDate,
                p.precio AS Price,
                COALESCE(GROUP_CONCAT(DISTINCT cp.categoriaId ORDER BY cp.categoriaId SEPARATOR '|'), '') AS CategoryIdsString
            FROM productos p
            LEFT JOIN categoriaDeProducto cp ON p.id = cp.productId
            WHERE p.id = @Id AND p.estado = @ActiveState
            GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio";

            var result = await connection.QueryFirstOrDefaultAsync<(long Id, string Name, string Description, int Stock, string Batch, DateTime ExpirationDate, decimal Price, string CategoryIdsString)>(
                new CommandDefinition(query, new { Id = id, ActiveState }, cancellationToken: cancellationToken));

            if (result == default)
            {
                return null;
            }

            return new Producto
            {
                Id = result.Id,
                Name = result.Name,
                Description = result.Description,
                Stock = result.Stock,
                Batch = result.Batch,
                ExpirationDate = DateOnly.FromDateTime(result.ExpirationDate),
                Price = result.Price,
                CategoryIds = ParseCategoryIds(result.CategoryIdsString)
            };
        }

        public async Task<long> CreateAsync(Producto product, CancellationToken cancellationToken = default)
        {
            using var connection = _factory.CreateConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string insertProductQuery = @"INSERT INTO productos (nombre, descripcion, stock, lote, fechaCaducidad, precio, estado) VALUES (@Name, @Description, @Stock, @Batch, @ExpirationDate, @Price, @ActiveState); SELECT LAST_INSERT_ID();";

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

                var normalizedCategoryIds = NormalizeCategoryIds(product.CategoryIds);
                if (normalizedCategoryIds.Count > 0)
                {
                    var insertCategoriesCommand = BuildInsertProductCategoriesCommand(
                        createdProductId, normalizedCategoryIds, transaction, cancellationToken);
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

        public async Task<int> UpdateAsync(Producto product, CancellationToken cancellationToken = default)
        {
            using var connection = _factory.CreateConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                var normalizedCategoryIds = NormalizeCategoryIds(product.CategoryIds);

                const string updateProductQuery = @"UPDATE productos
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

                const string deleteRelationsQuery = @"DELETE FROM categoriaDeProducto WHERE productId = @ProductId";
                var deleteRelationsCommand = new CommandDefinition(
                    deleteRelationsQuery,
                    parameters: new { ProductId = product.Id },
                    transaction: transaction,
                    cancellationToken: cancellationToken);
                await connection.ExecuteAsync(deleteRelationsCommand);

                if (normalizedCategoryIds.Count > 0)
                {
                    var insertCategoriesCommand = BuildInsertProductCategoriesCommand(
                        product.Id, normalizedCategoryIds, transaction, cancellationToken);
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

        public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = _factory.CreateConnection();
            const string query = @"UPDATE productos SET estado = @InactiveState WHERE id = @Id AND estado = @ActiveState";
            var command = new CommandDefinition(query, new { Id = id, ActiveState, InactiveState }, cancellationToken: cancellationToken);
            return await connection.ExecuteAsync(command);
        }

        private static async Task<IReadOnlyList<Producto>> GetProductsByIdsAsync(IDbConnection connection, IReadOnlyList<long> productIds, CancellationToken cancellationToken)
        {
            const string query = @"
                SELECT 
                    p.id as Id,
                    p.nombre as Name,
                    p.descripcion as Description,
                    p.stock as Stock,
                    p.lote as Batch,
                    p.fechaCaducidad as ExpirationDate,
                    p.precio as Price,
                    COALESCE(GROUP_CONCAT(DISTINCT cp.categoriaId ORDER BY cp.categoriaId SEPARATOR '|'), '') as CategoryIdsString
                FROM productos p
                LEFT JOIN categoriaDeProducto cp ON p.id = cp.productId
                WHERE p.estado = @ActiveState
                AND p.id IN @ProductIds
                GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio";

            var products = (await connection.QueryAsync<(long Id, string Name, string Description, int Stock, string Batch, DateTime ExpirationDate, decimal Price, string CategoryIdsString)>(
                new CommandDefinition(
                    query,
                    parameters: new { ActiveState, ProductIds = productIds },
                    cancellationToken: cancellationToken))).ToList();

            var result = new List<Producto>(products.Count);
            foreach (var p in products)
            {
                result.Add(new Producto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Stock = p.Stock,
                    Batch = p.Batch,
                    ExpirationDate = DateOnly.FromDateTime(p.ExpirationDate),
                    Price = p.Price,
                    CategoryIds = ParseCategoryIds(p.CategoryIdsString)
                });
            }

            return result;
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
                return new List<long>();
            }

            var categoryIds = new List<long>();
            var uniqueCategoryIds = new HashSet<long>();

            foreach (var rawCategoryId in categoryIdsString.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
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
            long productId, IReadOnlyList<long> normalizedCategoryIds, IDbTransaction transaction, CancellationToken cancellationToken)
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
                queryBuilder.Append("(@ProductId, @").Append(categoryParameterName).Append(')');

                parameters.Add(categoryParameterName, normalizedCategoryIds[index]);
            }

            return new CommandDefinition(
                queryBuilder.ToString(),
                parameters: parameters,
                transaction: transaction,
                cancellationToken: cancellationToken);
        }

        private static DateTime ToDateTime(DateOnly value) => value.ToDateTime(TimeOnly.MinValue);

        private static string BuildOrderByClause(string sortBy, string sortDirection, bool reverse = false)
        {
            var direction = NormalizeSortDirection(sortDirection);
            var effectiveDirection = direction;
            if (reverse)
            {
                if (string.Equals(direction, "ASC", StringComparison.Ordinal))
                {
                    effectiveDirection = "DESC";
                }
                else
                {
                    effectiveDirection = "ASC";
                }
            }

            var idTieDirection = "ASC";
            if (reverse)
            {
                idTieDirection = "DESC";
            }

            var normalizedSortBy = NormalizeSortBy(sortBy);

            return normalizedSortBy switch
            {
                "id" => $"p.id {effectiveDirection}",
                "stock" => $"p.stock {effectiveDirection}, p.id {idTieDirection}",
                "batch" => $"p.lote {effectiveDirection}, p.id {idTieDirection}",
                "expirationdate" => $"p.fechaCaducidad {effectiveDirection}, p.id {idTieDirection}",
                "price" => $"p.precio {effectiveDirection}, p.id {idTieDirection}",
                _ => $"p.nombre {effectiveDirection}, p.id {idTieDirection}"
            };
        }

        private static string NormalizeSortDirection(string sortDirection)
        {
            return string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        }

        private static string NormalizeSortBy(string sortBy)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return "name";
            }

            var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
            return normalizedSortBy switch
            {
                "id" => "id",
                "stock" => "stock",
                "batch" => "batch",
                "expirationdate" => "expirationdate",
                "price" => "price",
                _ => "name"
            };
        }

        private static string ResolveSortColumn(string normalizedSortBy)
        {
            return normalizedSortBy switch
            {
                "stock" => "stock",
                "batch" => "lote",
                "expirationdate" => "fechaCaducidad",
                "price" => "precio",
                _ => "nombre"
            };
        }

        private static string BuildKeysetComparator(string sortBy, string sortDirection, bool isNextPage)
        {
            var normalizedSortBy = NormalizeSortBy(sortBy);
            var direction = NormalizeSortDirection(sortDirection);
            var isAscending = string.Equals(direction, "ASC", StringComparison.Ordinal);

            if (string.Equals(normalizedSortBy, "id", StringComparison.Ordinal))
            {
                if (isNextPage)
                {
                    return isAscending ? "p.id > cursorProduct.id" : "p.id < cursorProduct.id";
                }
                return isAscending ? "p.id < cursorProduct.id" : "p.id > cursorProduct.id";
            }

            var sortColumn = ResolveSortColumn(normalizedSortBy);
            var mainComparator = ">";
            if (isNextPage)
            {
                if (!isAscending)
                {
                    mainComparator = "<";
                }
            }
            else
            {
                if (isAscending)
                {
                    mainComparator = "<";
                }
                else
                {
                    mainComparator = ">";
                }
            }

            var tieComparator = "<";
            if (isNextPage)
            {
                tieComparator = ">";
            }

            return $@"(
                p.{sortColumn} {mainComparator} cursorProduct.{sortColumn}
                OR (p.{sortColumn} = cursorProduct.{sortColumn} AND p.id {tieComparator} cursorProduct.id)
            )";
        }

        private static string BuildAnchorInclusiveComparator(string sortBy, string sortDirection)
        {
            var normalizedSortBy = NormalizeSortBy(sortBy);
            var direction = NormalizeSortDirection(sortDirection);
            var isAscending = string.Equals(direction, "ASC", StringComparison.Ordinal);

            if (string.Equals(normalizedSortBy, "id", StringComparison.Ordinal))
            {
                return isAscending ? "p.id >= anchor.id" : "p.id <= anchor.id";
            }

            var sortColumn = ResolveSortColumn(normalizedSortBy);
            var mainComparator = "<";
            if (isAscending)
            {
                mainComparator = ">";
            }

            return $@"(
                p.{sortColumn} {mainComparator} anchor.{sortColumn}
                OR (p.{sortColumn} = anchor.{sortColumn} AND p.id >= anchor.id)
            )";
        }
    }
}