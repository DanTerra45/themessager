using System.Data;
using System.Text;
using Dapper;
using Mercadito.database.interfaces;
using Mercadito.src.products.data.entity;
using Mercadito.src.products.domain.model;
using Mercadito.src.shared.domain.repository;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Mercadito.src.products.data.repository
{
    public class ProductRepository(IDbConnectionFactory dbConnection)
        : ICrudRepository<ProductWithCategoriesWriteModel, ProductWithCategoriesWriteModel, ProductForEditModel, long>
    {
        private const string ActiveState = "A";
        private const string InactiveState = "I";

        private readonly IDbConnectionFactory _dbConnection = dbConnection;

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

        private static CommandDefinition BuildCountActiveCategoriesCommand(
            IReadOnlyList<long> normalizedCategoryIds,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            const string query = @"SELECT COUNT(*)
                    FROM JSON_TABLE(@CategoryIdsJson, '$[*]' COLUMNS (CategoryId BIGINT PATH '$')) requested
                    INNER JOIN categorias c ON c.id = requested.CategoryId
                    WHERE c.estado = @ActiveState";

            return new CommandDefinition(
                query,
                parameters: new
                {
                    ActiveState,
                    CategoryIdsJson = JsonSerializer.Serialize(normalizedCategoryIds)
                },
                transaction: transaction,
                cancellationToken: cancellationToken);
        }

        private static async Task EnsureAllCategoriesAreActiveAsync(
            IDbConnection connection,
            IReadOnlyList<long> normalizedCategoryIds,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            if (normalizedCategoryIds.Count == 0)
            {
                throw new ValidationException("Debe seleccionar al menos una categoría activa.");
            }

            var activeCategoriesCommand = BuildCountActiveCategoriesCommand(
                normalizedCategoryIds,
                transaction,
                cancellationToken);

            var activeCategoriesCount = await connection.ExecuteScalarAsync<int>(activeCategoriesCommand);
            if (activeCategoriesCount != normalizedCategoryIds.Count)
            {
                throw new ValidationException("Una o más categorías seleccionadas no existen o están inactivas.");
            }
        }

        private static CommandDefinition BuildRelatedCategoryIdsByProductCommand(
            long productId,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            const string query = @"SELECT DISTINCT categoriaId
                    FROM categoriaDeProducto
                    WHERE productId = @ProductId";

            return new CommandDefinition(
                query,
                parameters: new { ProductId = productId },
                transaction: transaction,
                cancellationToken: cancellationToken);
        }

        private static IReadOnlyList<long> MergeCategoryIds(
            IReadOnlyList<long> firstCategoryIds,
            IReadOnlyList<long> secondCategoryIds)
        {
            var mergedCategoryIds = new List<long>(firstCategoryIds.Count + secondCategoryIds.Count);
            var uniqueCategoryIds = new HashSet<long>();

            foreach (var categoryId in firstCategoryIds)
            {
                if (categoryId <= 0 || !uniqueCategoryIds.Add(categoryId))
                {
                    continue;
                }

                mergedCategoryIds.Add(categoryId);
            }

            foreach (var categoryId in secondCategoryIds)
            {
                if (categoryId <= 0 || !uniqueCategoryIds.Add(categoryId))
                {
                    continue;
                }

                mergedCategoryIds.Add(categoryId);
            }

            return mergedCategoryIds;
        }

        private static async Task RecalculateCategoryProductCountsAsync(
            IDbConnection connection,
            IReadOnlyList<long> categoryIds,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            if (categoryIds.Count == 0)
            {
                return;
            }

            const string query = @"UPDATE categorias c
                    LEFT JOIN (
                        SELECT
                            cp.categoriaId AS CategoryId,
                            COUNT(DISTINCT cp.productId) AS ActiveProductCount
                        FROM categoriaDeProducto cp
                        INNER JOIN products p ON p.id = cp.productId
                        WHERE cp.categoriaId IN @CategoryIds
                        AND p.estado = @ActiveState
                        GROUP BY cp.categoriaId
                    ) counts ON counts.CategoryId = c.id
                    SET c.productosActivosCount = COALESCE(counts.ActiveProductCount, 0)
                    WHERE c.id IN @CategoryIds";

            var command = new CommandDefinition(
                query,
                parameters: new
                {
                    ActiveState,
                    CategoryIds = categoryIds
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }

        private static string BuildOrderByClause(string sortBy, string sortDirection, bool reverse = false)
        {
            var direction = NormalizeSortDirection(sortDirection);
            var effectiveDirection = reverse
                ? (string.Equals(direction, "ASC", StringComparison.Ordinal) ? "DESC" : "ASC")
                : direction;
            var idTieDirection = reverse ? "DESC" : "ASC";
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
            var mainComparator = isNextPage
                ? (isAscending ? ">" : "<")
                : (isAscending ? "<" : ">");
            var tieComparator = isNextPage ? ">" : "<";

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
            var mainComparator = isAscending ? ">" : "<";
            const string tieComparator = ">=";

            return $@"(
                p.{sortColumn} {mainComparator} anchor.{sortColumn}
                OR (p.{sortColumn} = anchor.{sortColumn} AND p.id {tieComparator} anchor.id)
            )";
        }

        private static string NormalizeSortDirection(string sortDirection)
        {
            return string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        }

        private static string NormalizeSearchTerm(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return string.Empty;
            }

            return searchTerm.Trim();
        }

        private static string BuildSearchPattern(string normalizedSearchTerm)
        {
            if (string.IsNullOrWhiteSpace(normalizedSearchTerm))
            {
                return string.Empty;
            }

            var escapedSearchTerm = normalizedSearchTerm
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);

            return $"%{escapedSearchTerm}%";
        }

        private static string BuildFullTextSearchQuery(string normalizedSearchTerm)
        {
            if (string.IsNullOrWhiteSpace(normalizedSearchTerm))
            {
                return string.Empty;
            }

            var terms = new List<string>();
            var rawTerms = normalizedSearchTerm.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawTerm in rawTerms)
            {
                var sanitizedBuilder = new StringBuilder(rawTerm.Length);
                foreach (var character in rawTerm)
                {
                    if (char.IsLetterOrDigit(character))
                    {
                        sanitizedBuilder.Append(character);
                    }
                }

                var sanitizedTerm = sanitizedBuilder.ToString();
                if (sanitizedTerm.Length < 3)
                {
                    continue;
                }

                terms.Add($"+{sanitizedTerm}*");
            }

            return terms.Count == 0
                ? string.Empty
                : string.Join(' ', terms);
        }

        private static void AppendSearchFilterByMatchedProductIds(StringBuilder queryBuilder, bool useFullTextSearch)
        {
            if (useFullTextSearch)
            {
                queryBuilder.Append(@" 
                AND p.id IN (
                    SELECT matched.ProductId
                    FROM (
                        SELECT p2.id AS ProductId
                        FROM products p2
                        WHERE p2.estado = @ActiveState
                        AND MATCH(p2.nombre) AGAINST (@SearchFullTextQuery IN BOOLEAN MODE)
                        UNION
                        SELECT pc2.productId AS ProductId
                        FROM categoriaDeProducto pc2
                        INNER JOIN categorias c2 ON c2.id = pc2.categoriaId
                        WHERE c2.estado = @ActiveState
                        AND MATCH(c2.nombre) AGAINST (@SearchFullTextQuery IN BOOLEAN MODE)
                    ) matched
                )");
                return;
            }

            queryBuilder.Append(@" 
                AND p.id IN (
                    SELECT matched.ProductId
                    FROM (
                        SELECT p2.id AS ProductId
                        FROM products p2
                        WHERE p2.estado = @ActiveState
                        AND p2.nombre LIKE @SearchPattern ESCAPE '\\'
                        UNION
                        SELECT pc2.productId AS ProductId
                        FROM categoriaDeProducto pc2
                        INNER JOIN categorias c2 ON c2.id = pc2.categoriaId
                        WHERE c2.estado = @ActiveState
                        AND c2.nombre LIKE @SearchPattern ESCAPE '\\'
                    ) matched
                )");
        }

        private static CommandDefinition BuildProductsByIdsCommand(
            IReadOnlyList<long> productIds,
            CancellationToken cancellationToken)
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
                    COALESCE(GROUP_CONCAT(DISTINCT c.nombre SEPARATOR ','), '') as CategoriesString
                FROM products p
                LEFT JOIN categoriaDeProducto pc ON p.id = pc.productId
                LEFT JOIN categorias c ON pc.categoriaId = c.id AND c.estado = @ActiveState
                WHERE p.estado = @ActiveState
                AND p.id IN @ProductIds
                GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio";

            return new CommandDefinition(
                query,
                parameters: new
                {
                    ActiveState,
                    ProductIds = productIds
                },
                cancellationToken: cancellationToken);
        }

        private static IReadOnlyList<ProductWithCategoriesModel> MapProductsWithCategoriesByIdOrder(
            IReadOnlyList<(
                long Id,
                string Name,
                string Description,
                int Stock,
                string Batch,
                DateTime ExpirationDate,
                decimal Price,
                string CategoriesString)> rows,
            IReadOnlyList<long> orderedProductIds)
        {
            var productsById = new Dictionary<long, ProductWithCategoriesModel>(rows.Count);
            foreach (var row in rows)
            {
                productsById[row.Id] = ToProductWithCategoriesModel(row);
            }

            var orderedProducts = new List<ProductWithCategoriesModel>(orderedProductIds.Count);
            foreach (var productId in orderedProductIds)
            {
                if (productsById.TryGetValue(productId, out var product))
                {
                    orderedProducts.Add(product);
                }
            }

            return orderedProducts;
        }

        private async Task<IReadOnlyList<ProductWithCategoriesModel>> GetProductsWithCategoriesByCursorInternalAsync(
            IDbConnection connection,
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorProductId,
            bool isNextPage,
            string searchTerm,
            long categoryId,
            bool filterByCategory,
            CancellationToken cancellationToken)
        {
            var normalizedSearchTerm = NormalizeSearchTerm(searchTerm);
            var hasSearch = normalizedSearchTerm.Length > 0;
            var fullTextSearchQuery = hasSearch ? BuildFullTextSearchQuery(normalizedSearchTerm) : string.Empty;
            var useFullTextSearch = !string.IsNullOrWhiteSpace(fullTextSearchQuery);
            var searchPattern = hasSearch ? BuildSearchPattern(normalizedSearchTerm) : string.Empty;
            var orderByClause = BuildOrderByClause(sortBy, sortDirection, reverse: !isNextPage);
            var keysetComparator = BuildKeysetComparator(sortBy, sortDirection, isNextPage);

            var productIdsQueryBuilder = new StringBuilder(@"
                SELECT p.id
                FROM products p
                INNER JOIN products cursorProduct ON cursorProduct.id = @CursorProductId");

            if (filterByCategory)
            {
                productIdsQueryBuilder.Append(@"
                INNER JOIN categoriaDeProducto pc ON p.id = pc.productId");
            }

            productIdsQueryBuilder.Append(@"
                WHERE p.estado = @ActiveState
                AND cursorProduct.estado = @ActiveState");

            if (filterByCategory)
            {
                productIdsQueryBuilder.Append(@"
                AND pc.categoriaId = @CategoryId");
            }

            if (hasSearch)
            {
                AppendSearchFilterByMatchedProductIds(productIdsQueryBuilder, useFullTextSearch);
            }

            productIdsQueryBuilder.Append(@"
                AND ");
            productIdsQueryBuilder.Append(keysetComparator);
            productIdsQueryBuilder.Append($@"
                ORDER BY {orderByClause}
                LIMIT @PageSize");

            var parameters = new DynamicParameters();
            parameters.Add("ActiveState", ActiveState);
            parameters.Add("CursorProductId", cursorProductId);
            parameters.Add("PageSize", pageSize);

            if (filterByCategory)
            {
                parameters.Add("CategoryId", categoryId);
            }

            if (hasSearch)
            {
                if (useFullTextSearch)
                {
                    parameters.Add("SearchFullTextQuery", fullTextSearchQuery);
                }
                else
                {
                    parameters.Add("SearchPattern", searchPattern);
                }
            }

            var productIdsCommand = new CommandDefinition(
                productIdsQueryBuilder.ToString(),
                parameters: parameters,
                cancellationToken: cancellationToken);

            var productIds = (await connection.QueryAsync<long>(productIdsCommand)).AsList();
            if (productIds.Count == 0)
            {
                return [];
            }

            if (!isNextPage)
            {
                productIds.Reverse();
            }

            var productsCommand = BuildProductsByIdsCommand(productIds, cancellationToken);
            var products = (await connection.QueryAsync<(long Id, string Name, string Description, int Stock, string Batch, DateTime ExpirationDate, decimal Price, string CategoriesString)>(
                productsCommand)).AsList();

            return MapProductsWithCategoriesByIdOrder(products, productIds);
        }

        public async Task<IReadOnlyList<ProductWithCategoriesModel>> GetProductsWithCategoriesByCursorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorProductId,
            bool isNextPage,
            string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            return await GetProductsWithCategoriesByCursorInternalAsync(
                connection,
                pageSize,
                sortBy,
                sortDirection,
                cursorProductId,
                isNextPage,
                searchTerm,
                categoryId: 0,
                filterByCategory: false,
                cancellationToken);
        }

        public async Task<IReadOnlyList<ProductWithCategoriesModel>> GetProductsWithCategoriesByCategoryCursorAsync(
            long categoryId,
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorProductId,
            bool isNextPage,
            string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            return await GetProductsWithCategoriesByCursorInternalAsync(
                connection,
                pageSize,
                sortBy,
                sortDirection,
                cursorProductId,
                isNextPage,
                searchTerm,
                categoryId,
                filterByCategory: true,
                cancellationToken);
        }

        public async Task<IReadOnlyList<ProductWithCategoriesModel>> GetProductsWithCategoriesFromAnchorAsync(
            long categoryId,
            int pageSize,
            string sortBy,
            string sortDirection,
            long anchorProductId,
            string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            var hasAnchor = anchorProductId > 0;
            var filterByCategory = categoryId > 0;
            var orderByClause = BuildOrderByClause(sortBy, sortDirection);
            var normalizedSearchTerm = NormalizeSearchTerm(searchTerm);
            var hasSearch = normalizedSearchTerm.Length > 0;
            var fullTextSearchQuery = hasSearch ? BuildFullTextSearchQuery(normalizedSearchTerm) : string.Empty;
            var useFullTextSearch = !string.IsNullOrWhiteSpace(fullTextSearchQuery);
            var searchPattern = hasSearch ? BuildSearchPattern(normalizedSearchTerm) : string.Empty;

            var productIdsQueryBuilder = new StringBuilder(@"
                SELECT p.id
                FROM products p");

            if (hasAnchor)
            {
                productIdsQueryBuilder.Append(@"
                INNER JOIN products anchor ON anchor.id = @AnchorProductId");
            }

            if (filterByCategory)
            {
                productIdsQueryBuilder.Append(@"
                INNER JOIN categoriaDeProducto pc ON p.id = pc.productId");
            }

            productIdsQueryBuilder.Append(@"
                WHERE p.estado = @ActiveState");

            if (hasAnchor)
            {
                productIdsQueryBuilder.Append(@"
                AND anchor.estado = @ActiveState
                AND ");
                productIdsQueryBuilder.Append(BuildAnchorInclusiveComparator(sortBy, sortDirection));
            }

            if (filterByCategory)
            {
                productIdsQueryBuilder.Append(@"
                AND pc.categoriaId = @CategoryId");
            }

            if (hasSearch)
            {
                AppendSearchFilterByMatchedProductIds(productIdsQueryBuilder, useFullTextSearch);
            }

            productIdsQueryBuilder.Append($@"
                ORDER BY {orderByClause}
                LIMIT @PageSize");

            var parameters = new DynamicParameters();
            parameters.Add("ActiveState", ActiveState);
            parameters.Add("PageSize", pageSize);

            if (hasAnchor)
            {
                parameters.Add("AnchorProductId", anchorProductId);
            }

            if (filterByCategory)
            {
                parameters.Add("CategoryId", categoryId);
            }

            if (hasSearch)
            {
                if (useFullTextSearch)
                {
                    parameters.Add("SearchFullTextQuery", fullTextSearchQuery);
                }
                else
                {
                    parameters.Add("SearchPattern", searchPattern);
                }
            }

            var productIdsCommand = new CommandDefinition(
                productIdsQueryBuilder.ToString(),
                parameters: parameters,
                cancellationToken: cancellationToken);

            var productIds = (await connection.QueryAsync<long>(productIdsCommand)).AsList();
            if (productIds.Count == 0)
            {
                return [];
            }

            var productsCommand = BuildProductsByIdsCommand(productIds, cancellationToken);
            var products = (await connection.QueryAsync<(long Id, string Name, string Description, int Stock, string Batch, DateTime ExpirationDate, decimal Price, string CategoriesString)>(
                productsCommand)).AsList();

            return MapProductsWithCategoriesByIdOrder(products, productIds);
        }

        public async Task<bool> HasProductsByCursorAsync(
            long categoryId,
            string sortBy,
            string sortDirection,
            long cursorProductId,
            bool isNextPage,
            string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            if (cursorProductId <= 0)
            {
                return false;
            }

            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            var filterByCategory = categoryId > 0;
            var normalizedSearchTerm = NormalizeSearchTerm(searchTerm);
            var hasSearch = normalizedSearchTerm.Length > 0;
            var fullTextSearchQuery = hasSearch ? BuildFullTextSearchQuery(normalizedSearchTerm) : string.Empty;
            var useFullTextSearch = !string.IsNullOrWhiteSpace(fullTextSearchQuery);
            var searchPattern = hasSearch ? BuildSearchPattern(normalizedSearchTerm) : string.Empty;
            var keysetComparator = BuildKeysetComparator(sortBy, sortDirection, isNextPage);
            var orderByClause = BuildOrderByClause(sortBy, sortDirection, reverse: !isNextPage);

            var queryBuilder = new StringBuilder(@"
                SELECT p.id
                FROM products p
                INNER JOIN products cursorProduct ON cursorProduct.id = @CursorProductId");

            if (filterByCategory)
            {
                queryBuilder.Append(@"
                INNER JOIN categoriaDeProducto pc ON p.id = pc.productId");
            }

            queryBuilder.Append(@"
                WHERE p.estado = @ActiveState
                AND cursorProduct.estado = @ActiveState");

            if (filterByCategory)
            {
                queryBuilder.Append(@"
                AND pc.categoriaId = @CategoryId");
            }

            if (hasSearch)
            {
                AppendSearchFilterByMatchedProductIds(queryBuilder, useFullTextSearch);
            }

            queryBuilder.Append(@"
                AND ");
            queryBuilder.Append(keysetComparator);
            queryBuilder.Append($@"
                ORDER BY {orderByClause}
                LIMIT 1");

            var parameters = new DynamicParameters();
            parameters.Add("CursorProductId", cursorProductId);
            parameters.Add("ActiveState", ActiveState);

            if (filterByCategory)
            {
                parameters.Add("CategoryId", categoryId);
            }

            if (hasSearch)
            {
                if (useFullTextSearch)
                {
                    parameters.Add("SearchFullTextQuery", fullTextSearchQuery);
                }
                else
                {
                    parameters.Add("SearchPattern", searchPattern);
                }
            }

            var command = new CommandDefinition(
                queryBuilder.ToString(),
                parameters: parameters,
                cancellationToken: cancellationToken);

            var nextProductId = await connection.QueryFirstOrDefaultAsync<long?>(command);
            return nextProductId.HasValue;
        }

        public async Task<ProductForEditModel?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
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

        public async Task<long> CreateAsync(ProductWithCategoriesWriteModel productWithCategories, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                var product = productWithCategories.Product;
                var normalizedCategoryIds = NormalizeCategoryIds(productWithCategories.CategoryIds);
                await EnsureAllCategoriesAreActiveAsync(
                    connection,
                    normalizedCategoryIds,
                    transaction,
                    cancellationToken);

                const string insertProductQuery = "INSERT INTO products (nombre, descripcion, stock, lote, fechaCaducidad, precio, estado) VALUES (@Name, @Description, @Stock, @Batch, @ExpirationDate, @Price, @ActiveState); SELECT LAST_INSERT_ID();";
                var insertProductCommand = new CommandDefinition(
                    insertProductQuery,
                    parameters: new
                    {
                        product.Name,
                        product.Description,
                        product.Stock,
                        product.Batch,
                        ExpirationDate = ToDateTime(product.ExpirationDate),
                        product.Price,
                        ActiveState
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var createdProductId = await connection.ExecuteScalarAsync<long>(insertProductCommand);

                if (normalizedCategoryIds.Count > 0)
                {
                    var insertCategoriesCommand = BuildInsertProductCategoriesCommand(
                        createdProductId,
                        normalizedCategoryIds,
                        transaction,
                        cancellationToken);

                    await connection.ExecuteAsync(insertCategoriesCommand);
                }

                await RecalculateCategoryProductCountsAsync(
                    connection,
                    normalizedCategoryIds,
                    transaction,
                    cancellationToken);

                transaction.Commit();
                return createdProductId;
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                transaction.Rollback();
                throw new ValidationException("Ya existe un producto activo con el mismo nombre, lote y fecha de caducidad.");
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                transaction.Rollback();
                throw new ValidationException("Los datos del producto no cumplen el formato requerido.");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> UpdateAsync(ProductWithCategoriesWriteModel productWithCategories, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                var product = productWithCategories.Product;
                var normalizedCategoryIds = NormalizeCategoryIds(productWithCategories.CategoryIds);
                var currentCategoryIdsCommand = BuildRelatedCategoryIdsByProductCommand(
                    product.Id,
                    transaction,
                    cancellationToken);
                var currentCategoryIds = (await connection.QueryAsync<long>(currentCategoryIdsCommand)).AsList();
                var touchedCategoryIds = MergeCategoryIds(currentCategoryIds, normalizedCategoryIds);

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
                        product.Batch,
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

                await EnsureAllCategoriesAreActiveAsync(
                    connection,
                    normalizedCategoryIds,
                    transaction,
                    cancellationToken);

                const string deleteRelationsQuery = @"DELETE FROM categoriaDeProducto
                    WHERE productId = @ProductId";

                var deleteRelationsCommand = new CommandDefinition(
                    deleteRelationsQuery,
                    parameters: new { ProductId = product.Id },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(deleteRelationsCommand);

                if (normalizedCategoryIds.Count > 0)
                {
                    var insertCategoriesCommand = BuildInsertProductCategoriesCommand(
                        product.Id,
                        normalizedCategoryIds,
                        transaction,
                        cancellationToken);

                    await connection.ExecuteAsync(insertCategoriesCommand);
                }

                await RecalculateCategoryProductCountsAsync(
                    connection,
                    touchedCategoryIds,
                    transaction,
                    cancellationToken);

                transaction.Commit();
                return affectedRows;
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                transaction.Rollback();
                throw new ValidationException("Ya existe un producto activo con el mismo nombre, lote y fecha de caducidad.");
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                transaction.Rollback();
                throw new ValidationException("Los datos del producto no cumplen el formato requerido.");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                var relatedCategoryIdsCommand = BuildRelatedCategoryIdsByProductCommand(
                    id,
                    transaction,
                    cancellationToken);
                var relatedCategoryIds = (await connection.QueryAsync<long>(relatedCategoryIdsCommand)).AsList();

                const string query = @"UPDATE products
                    SET estado = @InactiveState
                    WHERE id = @Id AND estado = @ActiveState";

                var command = new CommandDefinition(
                    query,
                    parameters: new { Id = id, ActiveState, InactiveState },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var affectedRows = await connection.ExecuteAsync(command);
                if (affectedRows == 0)
                {
                    transaction.Rollback();
                    return 0;
                }

                await RecalculateCategoryProductCountsAsync(
                    connection,
                    relatedCategoryIds,
                    transaction,
                    cancellationToken);

                transaction.Commit();
                return affectedRows;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

    }
}
