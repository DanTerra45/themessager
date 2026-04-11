using System.Data;
using System.Text;
using Dapper;
using Mercadito.src.products.application.models;
using MySqlConnector;
using Mercadito.src.shared.domain.validation;

namespace Mercadito.src.products.infrastructure.persistence
{
    public partial class ProductRepository
    {
        private static async Task<IReadOnlyList<ProductWithCategoriesModel>> GetProductsWithCategoriesByCursorInternalAsync(
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
            var normalizedSearchTerm = ValidationText.NormalizeTrimmed(searchTerm);
            var hasSearch = normalizedSearchTerm.Length > 0;
            var fullTextSearchQuery = string.Empty;
            if (hasSearch)
            {
                fullTextSearchQuery = BuildFullTextSearchQuery(normalizedSearchTerm);
            }
            var useFullTextSearch = !string.IsNullOrWhiteSpace(fullTextSearchQuery);
            var searchPattern = string.Empty;
            if (hasSearch)
            {
                searchPattern = BuildSearchPattern(normalizedSearchTerm);
            }
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
            productIdsQueryBuilder.Append(FormattableString.Invariant($@"
                ORDER BY {orderByClause}
                LIMIT @PageSize"));

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
            try
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
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar productos", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar productos", exception);
            }
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
            try
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
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar productos por categoría", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar productos por categoría", exception);
            }
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
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                var hasAnchor = anchorProductId > 0;
                var filterByCategory = categoryId > 0;
                var orderByClause = BuildOrderByClause(sortBy, sortDirection);
                var normalizedSearchTerm = ValidationText.NormalizeTrimmed(searchTerm);
                var hasSearch = normalizedSearchTerm.Length > 0;
                var fullTextSearchQuery = string.Empty;
                if (hasSearch)
                {
                    fullTextSearchQuery = BuildFullTextSearchQuery(normalizedSearchTerm);
                }
                var useFullTextSearch = !string.IsNullOrWhiteSpace(fullTextSearchQuery);
                var searchPattern = string.Empty;
                if (hasSearch)
                {
                    searchPattern = BuildSearchPattern(normalizedSearchTerm);
                }

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

            productIdsQueryBuilder.Append(FormattableString.Invariant($@"
                ORDER BY {orderByClause}
                LIMIT @PageSize"));

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

                var productIds = (await connection.QueryAsync<long>(productIdsCommand)).ToList();
                if (productIds.Count == 0)
                {
                    return [];
                }

                var productsCommand = BuildProductsByIdsCommand(productIds, cancellationToken);
                var products = (await connection.QueryAsync<(long Id, string Name, string Description, int Stock, string Batch, DateTime ExpirationDate, decimal Price, string CategoriesString)>(
                    productsCommand)).ToList();

                return MapProductsWithCategoriesByIdOrder(products, productIds);
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar productos", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar productos", exception);
            }
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

            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                var filterByCategory = categoryId > 0;
                var normalizedSearchTerm = ValidationText.NormalizeTrimmed(searchTerm);
                var hasSearch = normalizedSearchTerm.Length > 0;
                var fullTextSearchQuery = string.Empty;
                if (hasSearch)
                {
                    fullTextSearchQuery = BuildFullTextSearchQuery(normalizedSearchTerm);
                }
                var useFullTextSearch = !string.IsNullOrWhiteSpace(fullTextSearchQuery);
                var searchPattern = string.Empty;
                if (hasSearch)
                {
                    searchPattern = BuildSearchPattern(normalizedSearchTerm);
                }
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
            queryBuilder.Append(FormattableString.Invariant($@"
                ORDER BY {orderByClause}
                LIMIT 1"));

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
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar la navegación de productos", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar la navegación de productos", exception);
            }
        }

        public async Task<ProductForEditModel?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            try
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
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar el producto", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar el producto", exception);
            }
        }
    }
}
