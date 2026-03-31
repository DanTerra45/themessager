using Dapper;
using Mercadito.database.interfaces;
using Mercadito.src.categories.data.entity;
using Mercadito.src.categories.domain.model;
using Mercadito.src.shared.domain.repository;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;

namespace Mercadito.src.categories.data.repository
{
    public class CategoryRepository(IDbConnectionFactory dbConnection) : ICrudRepository<Category, Category, CategoryModel, long>
    {
        private const string ActiveState = "A";
        private const string InactiveState = "I";
        private const int CategoryCodeSequenceId = 1;
        private const int MaximumCategoryCodeNumber = 99999;

        private readonly IDbConnectionFactory _dbConnection = dbConnection;

        private static string FormatCategoryCode(int codeNumber)
        {
            return $"C{codeNumber:00000}";
        }

        private static async Task<int> GetFallbackNextCategoryCodeNumberAsync(
            IDbConnection connection,
            IDbTransaction? transaction,
            CancellationToken cancellationToken)
        {
            const string query = @"SELECT COALESCE(MAX(CAST(SUBSTRING(codigo, 2, 5) AS UNSIGNED)), 0) + 1
                        FROM categorias";

            var command = new CommandDefinition(
                query,
                transaction: transaction,
                cancellationToken: cancellationToken);

            var nextCodeNumber = await connection.ExecuteScalarAsync<int>(command);
            return nextCodeNumber < 1 ? 1 : nextCodeNumber;
        }

        private static async Task<string> ReserveNextCategoryCodeAsync(
            IDbConnection connection,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            const string ensureSequenceRowQuery = @"INSERT INTO category_code_sequence (`id`, `nextValue`)
                        VALUES (@SequenceId, 1)
                        ON DUPLICATE KEY UPDATE `nextValue` = `nextValue`";

            var ensureSequenceRowCommand = new CommandDefinition(
                ensureSequenceRowQuery,
                parameters: new { SequenceId = CategoryCodeSequenceId },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(ensureSequenceRowCommand);

            const string lockAndReadQuery = @"SELECT `nextValue`
                        FROM category_code_sequence
                        WHERE `id` = @SequenceId
                        FOR UPDATE";

            var lockAndReadCommand = new CommandDefinition(
                lockAndReadQuery,
                parameters: new { SequenceId = CategoryCodeSequenceId },
                transaction: transaction,
                cancellationToken: cancellationToken);

            var reservedCodeNumber = await connection.ExecuteScalarAsync<int?>(lockAndReadCommand);
            var nextCodeNumber = reservedCodeNumber.GetValueOrDefault();
            if (nextCodeNumber <= 0)
            {
                nextCodeNumber = await GetFallbackNextCategoryCodeNumberAsync(connection, transaction, cancellationToken);
            }

            if (nextCodeNumber > MaximumCategoryCodeNumber)
            {
                throw new ValidationException("No hay más códigos de categoría disponibles.");
            }

            const string updateSequenceQuery = @"UPDATE category_code_sequence
                        SET `nextValue` = @FollowingCodeNumber
                        WHERE `id` = @SequenceId";

            var updateSequenceCommand = new CommandDefinition(
                updateSequenceQuery,
                parameters: new
                {
                    SequenceId = CategoryCodeSequenceId,
                    FollowingCodeNumber = nextCodeNumber + 1
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(updateSequenceCommand);
            return FormatCategoryCode(nextCodeNumber);
        }

        private static void TryRollback(IDbTransaction? transaction)
        {
            if (transaction?.Connection != null)
            {
                transaction.Rollback();
            }
        }

        private static string BuildCategoryRowsQuery()
        {
            return @"SELECT
                        c.id AS Id,
                        c.codigo AS Code,
                        c.nombre AS Name,
                        c.descripcion AS Description,
                        c.productosActivosCount AS ProductCount
                    FROM categorias c
                    WHERE c.estado = @ActiveState";
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
                "id" => $"currentCategory.Id {effectiveDirection}",
                "code" => $"currentCategory.Code {effectiveDirection}, currentCategory.Id {idTieDirection}",
                "productcount" => $"currentCategory.ProductCount {effectiveDirection}, currentCategory.Id {idTieDirection}",
                _ => $"currentCategory.Name {effectiveDirection}, currentCategory.Id {idTieDirection}"
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
            var mainComparator = isNextPage
                ? (isAscending ? ">" : "<")
                : (isAscending ? "<" : ">");
            var idComparator = isNextPage ? ">" : "<";

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
            var mainComparator = isAscending ? ">" : "<";

            return $@"(
                currentCategory.{sortColumn} {mainComparator} anchorCategory.{sortColumn}
                OR (currentCategory.{sortColumn} = anchorCategory.{sortColumn} AND currentCategory.Id >= anchorCategory.Id)
            )";
        }

        public async Task<IReadOnlyList<CategoryModel>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"SELECT
                        id AS Id,
                        codigo AS Code,
                        nombre AS Name,
                        descripcion AS Description,
                        productosActivosCount AS ProductCount
                        FROM categorias
                        WHERE estado = @ActiveState
                        ORDER BY nombre ASC";

            var command = new CommandDefinition(query, parameters: new { ActiveState }, cancellationToken: cancellationToken);
            var categories = await connection.QueryAsync<CategoryModel>(command);
            return categories.AsList();
        }

        public async Task<string> GetNextCategoryCodeAsync(CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            int nextCodeNumber;

            try
            {
                const string query = @"SELECT `nextValue`
                        FROM category_code_sequence
                        WHERE `id` = @SequenceId";

                var command = new CommandDefinition(
                    query,
                    parameters: new { SequenceId = CategoryCodeSequenceId },
                    cancellationToken: cancellationToken);

                var codeNumberFromSequence = await connection.ExecuteScalarAsync<int?>(command);
                if (codeNumberFromSequence.HasValue && codeNumberFromSequence.Value > 0)
                {
                    nextCodeNumber = codeNumberFromSequence.Value;
                }
                else
                {
                    nextCodeNumber = await GetFallbackNextCategoryCodeNumberAsync(connection, transaction: null, cancellationToken);
                }
            }
            catch (MySqlException exception) when (exception.Number == 1146)
            {
                nextCodeNumber = await GetFallbackNextCategoryCodeNumberAsync(connection, transaction: null, cancellationToken);
            }

            if (nextCodeNumber > MaximumCategoryCodeNumber)
            {
                throw new ValidationException("No hay más códigos de categoría disponibles.");
            }

            return FormatCategoryCode(nextCodeNumber);
        }

        public async Task<IReadOnlyList<CategoryModel>> GetCategoriesByCursorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorCategoryId,
            bool isNextPage,
            CancellationToken cancellationToken = default)
        {
            if (cursorCategoryId <= 0)
            {
                return [];
            }

            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            var orderByClause = BuildOrderByClause(sortBy, sortDirection, reverse: !isNextPage);
            var keysetComparator = BuildKeysetComparator(sortBy, sortDirection, isNextPage);

            var queryBuilder = new StringBuilder();
            queryBuilder.Append("SELECT currentCategory.Id, currentCategory.Code, currentCategory.Name, currentCategory.Description, currentCategory.ProductCount FROM (");
            queryBuilder.Append(BuildCategoryRowsQuery());
            queryBuilder.Append(") currentCategory INNER JOIN (");
            queryBuilder.Append(BuildCategoryRowsQuery());
            queryBuilder.Append(") cursorCategory ON cursorCategory.Id = @CursorCategoryId WHERE ");
            queryBuilder.Append(keysetComparator);
            queryBuilder.Append($@" ORDER BY {orderByClause}
                LIMIT @PageSize");

            var command = new CommandDefinition(
                queryBuilder.ToString(),
                parameters: new
                {
                    ActiveState,
                    CursorCategoryId = cursorCategoryId,
                    PageSize = pageSize
                },
                cancellationToken: cancellationToken);

            var categories = (await connection.QueryAsync<CategoryModel>(command)).AsList();
            if (!isNextPage)
            {
                categories.Reverse();
            }

            return categories;
        }

        public async Task<IReadOnlyList<CategoryModel>> GetCategoriesFromAnchorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long anchorCategoryId,
            CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            var hasAnchor = anchorCategoryId > 0;
            var orderByClause = BuildOrderByClause(sortBy, sortDirection);

            var queryBuilder = new StringBuilder();
            queryBuilder.Append("SELECT currentCategory.Id, currentCategory.Code, currentCategory.Name, currentCategory.Description, currentCategory.ProductCount FROM (");
            queryBuilder.Append(BuildCategoryRowsQuery());
            queryBuilder.Append(") currentCategory");

            if (hasAnchor)
            {
                queryBuilder.Append(" INNER JOIN (");
                queryBuilder.Append(BuildCategoryRowsQuery());
                queryBuilder.Append(") anchorCategory ON anchorCategory.Id = @AnchorCategoryId WHERE ");
                queryBuilder.Append(BuildAnchorInclusiveComparator(sortBy, sortDirection));
            }

            queryBuilder.Append($@" ORDER BY {orderByClause}
                LIMIT @PageSize");

            var parameters = new DynamicParameters();
            parameters.Add("ActiveState", ActiveState);
            parameters.Add("PageSize", pageSize);
            if (hasAnchor)
            {
                parameters.Add("AnchorCategoryId", anchorCategoryId);
            }

            var command = new CommandDefinition(
                queryBuilder.ToString(),
                parameters: parameters,
                cancellationToken: cancellationToken);

            var categories = await connection.QueryAsync<CategoryModel>(command);
            return categories.AsList();
        }

        public async Task<bool> HasCategoriesByCursorAsync(
            string sortBy,
            string sortDirection,
            long cursorCategoryId,
            bool isNextPage,
            CancellationToken cancellationToken = default)
        {
            if (cursorCategoryId <= 0)
            {
                return false;
            }

            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            var orderByClause = BuildOrderByClause(sortBy, sortDirection, reverse: !isNextPage);
            var keysetComparator = BuildKeysetComparator(sortBy, sortDirection, isNextPage);

            var queryBuilder = new StringBuilder();
            queryBuilder.Append("SELECT currentCategory.Id FROM (");
            queryBuilder.Append(BuildCategoryRowsQuery());
            queryBuilder.Append(") currentCategory INNER JOIN (");
            queryBuilder.Append(BuildCategoryRowsQuery());
            queryBuilder.Append(") cursorCategory ON cursorCategory.Id = @CursorCategoryId WHERE ");
            queryBuilder.Append(keysetComparator);
            queryBuilder.Append($@" ORDER BY {orderByClause}
                LIMIT 1");

            var command = new CommandDefinition(
                queryBuilder.ToString(),
                parameters: new
                {
                    ActiveState,
                    CursorCategoryId = cursorCategoryId
                },
                cancellationToken: cancellationToken);

            var nextCategoryId = await connection.QueryFirstOrDefaultAsync<long?>(command);
            return nextCategoryId.HasValue;
        }

        public async Task<CategoryModel?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"SELECT
                        c.id AS Id,
                        c.codigo AS Code,
                        c.nombre AS Name,
                        c.descripcion AS Description,
                        c.productosActivosCount AS ProductCount
                        FROM categorias c
                        WHERE c.id = @Id AND c.estado = @ActiveState";

            var command = new CommandDefinition(
                query,
                parameters: new { Id = id, ActiveState },
                cancellationToken: cancellationToken);

            return await connection.QueryFirstOrDefaultAsync<CategoryModel>(command);
        }

        public async Task<long> CreateAsync(Category category, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            IDbTransaction? transaction = null;

            try
            {
                transaction = connection.BeginTransaction();
                var reservedCode = await ReserveNextCategoryCodeAsync(connection, transaction, cancellationToken);

                const string query = @"INSERT INTO categorias
                        (codigo, nombre, descripcion, estado) VALUES (@Code, @Name, @Description, @ActiveState);
                        SELECT LAST_INSERT_ID();";

                var command = new CommandDefinition(
                    query,
                    parameters: new { Code = reservedCode, category.Name, category.Description, ActiveState },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var createdId = await connection.ExecuteScalarAsync<long>(command);
                transaction.Commit();
                return createdId;
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                TryRollback(transaction);
                throw new ValidationException("Ya existe una categoría con ese código.");
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                TryRollback(transaction);
                throw new ValidationException("Los datos de la categoría no cumplen el formato requerido.");
            }
            catch
            {
                TryRollback(transaction);
                throw;
            }
            finally
            {
                transaction?.Dispose();
            }
        }

        public async Task<int> UpdateAsync(Category category, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            try
            {
                const string query = @"UPDATE categorias
                        SET codigo = @Code, nombre = @Name, descripcion = @Description
                        WHERE id = @Id AND estado = @ActiveState";

                var command = new CommandDefinition(
                    query,
                    parameters: new { category.Id, category.Code, category.Name, category.Description, ActiveState },
                    cancellationToken: cancellationToken);

                return await connection.ExecuteAsync(command);
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                throw new ValidationException("Ya existe una categoría con ese código.");
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                throw new ValidationException("Los datos de la categoría no cumplen el formato requerido.");
            }
        }

        public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"UPDATE categorias
                    SET estado = @InactiveState
                    WHERE id = @Id AND estado = @ActiveState";
            var command = new CommandDefinition(
                query,
                parameters: new { Id = id, ActiveState, InactiveState },
                cancellationToken: cancellationToken);

            return await connection.ExecuteAsync(command);
        }
    }
}
