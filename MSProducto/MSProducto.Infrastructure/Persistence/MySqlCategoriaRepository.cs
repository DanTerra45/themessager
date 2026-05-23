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

    public class MySqlCategoriaRepository : ICategoriaRepository
    {
        private const int ActiveState = 1;
        private const int InactiveState = 0;
        private readonly IDbConnectionFactory _factory;

        public MySqlCategoriaRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<Categoria>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
        {
            using var connection = _factory.CreateConnection();
            const string query = @"
SELECT id AS Id, codigo AS Code, nombre AS Name, descripcion AS Description, productosActivosCount AS ProductCount
FROM categorias
WHERE estado = @ActiveState
ORDER BY nombre ASC";

            var categories = await connection.QueryAsync<Categoria>(new CommandDefinition(
                query, new { ActiveState }, cancellationToken: cancellationToken));
            return categories.AsList();
        }

        public async Task<string> GetNextCategoryCodeAsync(CancellationToken cancellationToken = default)
        {
            using var connection = _factory.CreateConnection();
            const string query = @"SELECT COALESCE(MAX(CAST(SUBSTRING(codigo, 2, 5) AS UNSIGNED)), 0) + 1 FROM categorias";
            var nextCodeNumber = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                query, cancellationToken: cancellationToken));

            return $"C{nextCodeNumber:00000}";
        }

        public async Task<IReadOnlyList<Categoria>> GetCategoriesByCursorAsync(
            int pageSize, string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage,
            string searchTerm, CancellationToken cancellationToken = default)
        {
            if (cursorCategoryId <= 0)
            {
                return new List<Categoria>();
            }

            using var connection = _factory.CreateConnection();

            var normalizedSearchTerm = (searchTerm ?? string.Empty).Trim();
            var hasSearchTerm = !string.IsNullOrWhiteSpace(normalizedSearchTerm);

            var orderByClause = BuildOrderByClause(sortBy, sortDirection, isNextPage);
            var keysetComparator = BuildKeysetComparator(sortBy, sortDirection, isNextPage);

            var parameters = new DynamicParameters();
            parameters.Add("ActiveState", ActiveState);
            parameters.Add("CursorCategoryId", cursorCategoryId);
            parameters.Add("PageSize", pageSize);

            if (hasSearchTerm)
            {
                parameters.Add("SearchPattern", "%" + normalizedSearchTerm + "%");
            }

            var queryBuilder = new StringBuilder();
            queryBuilder.Append(@"SELECT currentCategory.Id, currentCategory.Code, currentCategory.Name, currentCategory.Description, currentCategory.ProductCount FROM (");
            queryBuilder.Append(BuildCategoryRowsQuery(hasSearchTerm));
            queryBuilder.Append(") currentCategory INNER JOIN (");
            queryBuilder.Append(BuildCategoryRowsQuery(hasSearchTerm));
            queryBuilder.Append(") cursorCategory ON cursorCategory.Id = @CursorCategoryId WHERE ");
            queryBuilder.Append(keysetComparator);
            queryBuilder.Append($" ORDER BY {orderByClause} LIMIT @PageSize");

            var categories = await connection.QueryAsync<Categoria>(new CommandDefinition(
                queryBuilder.ToString(), parameters, cancellationToken: cancellationToken));

            var list = categories.AsList();
            if (!isNextPage)
            {
                list.Reverse();
            }
            return list;
        }

        public async Task<IReadOnlyList<Categoria>> GetCategoriesFromAnchorAsync(
            int pageSize, string sortBy, string sortDirection, long anchorCategoryId,
            string searchTerm, CancellationToken cancellationToken = default)
        {
            using var connection = _factory.CreateConnection();

            var normalizedSearchTerm = (searchTerm ?? string.Empty).Trim();
            var hasSearchTerm = !string.IsNullOrWhiteSpace(normalizedSearchTerm);
            var hasAnchor = anchorCategoryId > 0;

            var orderByClause = BuildOrderByClause(sortBy, sortDirection);

            var parameters = new DynamicParameters();
            parameters.Add("ActiveState", ActiveState);
            parameters.Add("PageSize", pageSize);

            if (hasSearchTerm)
            {
                parameters.Add("SearchPattern", "%" + normalizedSearchTerm + "%");
            }

            if (hasAnchor)
            {
                parameters.Add("AnchorCategoryId", anchorCategoryId);
            }

            var queryBuilder = new StringBuilder();
            queryBuilder.Append(@"SELECT currentCategory.Id, currentCategory.Code, currentCategory.Name, currentCategory.Description, currentCategory.ProductCount FROM (");
            queryBuilder.Append(BuildCategoryRowsQuery(hasSearchTerm));
            queryBuilder.Append(") currentCategory");

            if (hasAnchor)
            {
                queryBuilder.Append(" INNER JOIN (");
                queryBuilder.Append(BuildCategoryRowsQuery(hasSearchTerm));
                queryBuilder.Append(") anchorCategory ON anchorCategory.Id = @AnchorCategoryId WHERE ");
                queryBuilder.Append(BuildAnchorInclusiveComparator(sortBy, sortDirection));
            }

            queryBuilder.Append($" ORDER BY {orderByClause} LIMIT @PageSize");

            var categories = await connection.QueryAsync<Categoria>(new CommandDefinition(
                queryBuilder.ToString(), parameters, cancellationToken: cancellationToken));

            return categories.AsList();
        }

        public async Task<bool> HasCategoriesByCursorAsync(
            string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage,
            string searchTerm, CancellationToken cancellationToken = default)
        {
            if (cursorCategoryId <= 0)
            {
                return false;
            }

            using var connection = _factory.CreateConnection();

            var normalizedSearchTerm = (searchTerm ?? string.Empty).Trim();
            var hasSearchTerm = !string.IsNullOrWhiteSpace(normalizedSearchTerm);
            var orderByClause = BuildOrderByClause(sortBy, sortDirection, isNextPage);
            var keysetComparator = BuildKeysetComparator(sortBy, sortDirection, isNextPage);

            var parameters = new DynamicParameters();
            parameters.Add("ActiveState", ActiveState);
            parameters.Add("CursorCategoryId", cursorCategoryId);

            if (hasSearchTerm)
            {
                parameters.Add("SearchPattern", "%" + normalizedSearchTerm + "%");
            }

            var queryBuilder = new StringBuilder();
            queryBuilder.Append(@"SELECT currentCategory.Id FROM (");
            queryBuilder.Append(BuildCategoryRowsQuery(hasSearchTerm));
            queryBuilder.Append(") currentCategory INNER JOIN (");
            queryBuilder.Append(BuildCategoryRowsQuery(hasSearchTerm));
            queryBuilder.Append(") cursorCategory ON cursorCategory.Id = @CursorCategoryId WHERE ");
            queryBuilder.Append(keysetComparator);
            queryBuilder.Append($" ORDER BY {orderByClause} LIMIT 1");

            var nextCategoryId = await connection.QueryFirstOrDefaultAsync<long?>(new CommandDefinition(
                queryBuilder.ToString(), parameters, cancellationToken: cancellationToken));
            return nextCategoryId.HasValue;
        }

        public async Task<Categoria?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = _factory.CreateConnection();
            const string query = @"
SELECT id AS Id, codigo AS Code, nombre AS Name, descripcion AS Description, productosActivosCount AS ProductCount
FROM categorias
WHERE id = @Id AND estado = @ActiveState";

            var category = await connection.QueryFirstOrDefaultAsync<Categoria>(new CommandDefinition(
                query, new { Id = id, ActiveState }, cancellationToken: cancellationToken));

            return category;
        }

        public async Task<long> CreateAsync(Categoria category, CancellationToken cancellationToken = default)
        {
            using var connection = _factory.CreateConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                var nextCodeNumber = await GetNextCodeNumberAsync(connection, transaction, cancellationToken);

                const string query = @"
INSERT INTO categorias (codigo, nombre, descripcion, estado)
VALUES (@Code, @Name, @Description, @ActiveState);
SELECT LAST_INSERT_ID();";

                var createdId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
                    query, new { Code = $"C{nextCodeNumber:00000}", category.Name, category.Description, ActiveState },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                transaction.Commit();
                return createdId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> UpdateAsync(Categoria category, CancellationToken cancellationToken = default)
        {
            using var connection = _factory.CreateConnection();
            const string query = @"
UPDATE categorias
SET codigo = @Code, nombre = @Name, descripcion = @Description
WHERE id = @Id AND estado = @ActiveState";

            var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
                query, new { category.Id, category.Code, category.Name, category.Description, ActiveState },
                cancellationToken: cancellationToken));

            return affectedRows;
        }

        public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = _factory.CreateConnection();
            const string query = @"
UPDATE categorias
SET estado = @InactiveState
WHERE id = @Id AND estado = @ActiveState";

            var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
                query, new { Id = id, ActiveState, InactiveState },
                cancellationToken: cancellationToken));

            return affectedRows;
        }

        private static async Task<int> GetNextCodeNumberAsync(IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
        {
            const string query = @"SELECT COALESCE(MAX(CAST(SUBSTRING(codigo, 2, 5) AS UNSIGNED)), 0) + 1 FROM categorias";
            return await connection.ExecuteScalarAsync<int>(new CommandDefinition(query, transaction: transaction, cancellationToken: cancellationToken));
        }

        private static string BuildCategoryRowsQuery(bool hasSearchTerm)
        {
            var queryBuilder = new StringBuilder(@"SELECT
                c.id AS Id,
                c.codigo AS Code,
                c.nombre AS Name,
                c.descripcion AS Description,
                c.productosActivosCount AS ProductCount
            FROM categorias c
            WHERE c.estado = @ActiveState");

            if (hasSearchTerm)
            {
                queryBuilder.Append(@"
                AND (
                    c.codigo LIKE @SearchPattern
                    OR c.nombre LIKE @SearchPattern
                    OR c.descripcion LIKE @SearchPattern
                )");
            }

            return queryBuilder.ToString();
        }

        private static string BuildOrderByClause(string sortBy, string sortDirection, bool reverse = false)
        {
            var direction = NormalizeSortDirection(sortDirection);
            var effectiveDirection = direction;
            if (reverse)
            {
                effectiveDirection = string.Equals(direction, "ASC", StringComparison.Ordinal) ? "DESC" : "ASC";
            }

            var idTieDirection = "ASC";
            if (reverse)
            {
                idTieDirection = "DESC";
            }

            var normalizedSortBy = NormalizeSortBy(sortBy);

            return normalizedSortBy switch
            {
                "id" => $"currentCategory.Id {effectiveDirection}",
                "code" => $"currentCategory.Code {effectiveDirection}, currentCategory.Id {idTieDirection}",
                "productcount" => $"currentCategory.ProductCount {effectiveDirection}, currentCategory.Id {idTieDirection}",
                _ => $"currentCategory.Name {effectiveDirection}, currentCategory.Id {idTieDirection}"
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
                "code" => "code",
                "productcount" => "productcount",
                _ => "name"
            };
        }

        private static string ResolveSortColumn(string normalizedSortBy)
        {
            return normalizedSortBy switch
            {
                "id" => "Id",
                "code" => "Code",
                "productcount" => "ProductCount",
                _ => "Name"
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
                    return isAscending ? "currentCategory.Id > cursorCategory.Id" : "currentCategory.Id < cursorCategory.Id";
                }
                return isAscending ? "currentCategory.Id < cursorCategory.Id" : "currentCategory.Id > cursorCategory.Id";
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
            }

            var idComparator = "<";
            if (isNextPage)
            {
                idComparator = ">";
            }

            return $@"(
                currentCategory.{sortColumn} {mainComparator} cursorCategory.{sortColumn}
                OR (currentCategory.{sortColumn} = cursorCategory.{sortColumn} AND currentCategory.Id {idComparator} cursorCategory.Id)
            )";
        }

        private static string BuildAnchorInclusiveComparator(string sortBy, string sortDirection)
        {
            var normalizedSortBy = NormalizeSortBy(sortBy);
            var direction = NormalizeSortDirection(sortDirection);
            var isAscending = string.Equals(direction, "ASC", StringComparison.Ordinal);

            if (string.Equals(normalizedSortBy, "id", StringComparison.Ordinal))
            {
                return isAscending ? "currentCategory.Id >= anchorCategory.Id" : "currentCategory.Id <= anchorCategory.Id";
            }

            var sortColumn = ResolveSortColumn(normalizedSortBy);
            var mainComparator = "<";
            if (isAscending)
            {
                mainComparator = ">";
            }

            return $@"(
                currentCategory.{sortColumn} {mainComparator} anchorCategory.{sortColumn}
                OR (currentCategory.{sortColumn} = anchorCategory.{sortColumn} AND currentCategory.Id >= anchorCategory.Id)
            )";
        }
    }
}