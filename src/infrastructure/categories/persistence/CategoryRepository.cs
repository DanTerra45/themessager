using Mercadito.src.application.categories.ports.output;
using Mercadito.src.application.products.ports.output;
using Dapper;
using Mercadito.src.shared.infrastructure.persistence;
using Mercadito.src.domain.categories.entities;
using Mercadito.src.application.categories.models;
using Mercadito.src.domain.shared.repository;
using MySqlConnector;
using System.Data;
using System.Text;
using Mercadito.src.domain.shared.exceptions;
using Mercadito.src.domain.shared.validation;

namespace Mercadito.src.infrastructure.categories.persistence
{
    public class CategoryRepository(IDbConnectionFactory dbConnection) : ICategoryRepository, IProductCategoryLookupRepository, ICrudRepository<Category, Category, CategoryModel, long>
    {
        private const string ActiveState = "A";
        private const string InactiveState = "I";
        private const int CategoryCodeSequenceId = 1;
        private const int MaximumCategoryCodeNumber = 99999;

        private readonly IDbConnectionFactory _dbConnection = dbConnection;

        private static DataStoreUnavailableException CreateDataStoreUnavailableException(string operation, Exception exception)
        {
            return new DataStoreUnavailableException($"No se pudo {operation} porque la base de datos no está disponible.", exception);
        }

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
            if (nextCodeNumber < 1)
            {
                return 1;
            }

            return nextCodeNumber;
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
                throw new BusinessValidationException("Code", "No hay más códigos de categoría disponibles.");
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

        private static DynamicParameters BuildCategoryQueryParameters(string searchTerm, int? pageSize = null, long? cursorCategoryId = null, long? anchorCategoryId = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("ActiveState", ActiveState);

            var normalizedSearchTerm = ValidationText.NormalizeTrimmed(searchTerm);
            if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
            {
                parameters.Add("SearchPattern", "%" + normalizedSearchTerm + "%");
            }

            if (pageSize.HasValue)
            {
                parameters.Add("PageSize", pageSize.Value);
            }

            if (cursorCategoryId.HasValue)
            {
                parameters.Add("CursorCategoryId", cursorCategoryId.Value);
            }

            if (anchorCategoryId.HasValue)
            {
                parameters.Add("AnchorCategoryId", anchorCategoryId.Value);
            }

            return parameters;
        }

        private static string NormalizeSortDirection(string sortDirection)
        {
            if (string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            {
                return "DESC";
            }

            return "ASC";
        }

        private static string NormalizeSortBy(string sortBy)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return "name";
            }

            var normalizedSortBy = ValidationText.NormalizeLowerTrimmed(sortBy);
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
                    if (isAscending)
                    {
                        return "currentCategory.Id > cursorCategory.Id";
                    }

                    return "currentCategory.Id < cursorCategory.Id";
                }

                if (isAscending)
                {
                    return "currentCategory.Id < cursorCategory.Id";
                }

                return "currentCategory.Id > cursorCategory.Id";
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
                if (isAscending)
                {
                    return "currentCategory.Id >= anchorCategory.Id";
                }

                return "currentCategory.Id <= anchorCategory.Id";
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

        public async Task<IReadOnlyList<CategoryModel>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
        {
            try
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
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar categorías", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar categorías", exception);
            }
        }

        public async Task<string> GetNextCategoryCodeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                int nextCodeNumber;
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
                if (nextCodeNumber > MaximumCategoryCodeNumber)
                {
                    throw new BusinessValidationException("Code", "No hay más códigos de categoría disponibles.");
                }

                return FormatCategoryCode(nextCodeNumber);
            }
            catch (MySqlException exception) when (exception.Number == 1146)
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                var nextCodeNumber = await GetFallbackNextCategoryCodeNumberAsync(connection, transaction: null, cancellationToken);
                if (nextCodeNumber > MaximumCategoryCodeNumber)
                {
                    throw new BusinessValidationException("Code", "No hay más códigos de categoría disponibles.");
                }

                return FormatCategoryCode(nextCodeNumber);
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("obtener el siguiente código de categoría", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("obtener el siguiente código de categoría", exception);
            }
        }

        public async Task<IReadOnlyList<CategoryModel>> GetCategoriesByCursorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorCategoryId,
            bool isNextPage,
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            if (cursorCategoryId <= 0)
            {
                return [];
            }

            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                var orderByClause = BuildOrderByClause(sortBy, sortDirection, reverse: !isNextPage);
                var keysetComparator = BuildKeysetComparator(sortBy, sortDirection, isNextPage);
                var hasSearchTerm = !string.IsNullOrWhiteSpace(ValidationText.NormalizeTrimmed(searchTerm));

                var queryBuilder = new StringBuilder();
                queryBuilder.Append("SELECT currentCategory.Id, currentCategory.Code, currentCategory.Name, currentCategory.Description, currentCategory.ProductCount FROM (");
                queryBuilder.Append(BuildCategoryRowsQuery(hasSearchTerm));
                queryBuilder.Append(") currentCategory INNER JOIN (");
                queryBuilder.Append(BuildCategoryRowsQuery(hasSearchTerm));
                queryBuilder.Append(") cursorCategory ON cursorCategory.Id = @CursorCategoryId WHERE ");
                queryBuilder.Append(keysetComparator);
                queryBuilder.Append(FormattableString.Invariant($@" ORDER BY {orderByClause}
                LIMIT @PageSize"));

                var command = new CommandDefinition(
                queryBuilder.ToString(),
                parameters: BuildCategoryQueryParameters(searchTerm, pageSize, cursorCategoryId: cursorCategoryId),
                cancellationToken: cancellationToken);

                var categories = (await connection.QueryAsync<CategoryModel>(command)).AsList();
                if (!isNextPage)
                {
                    categories.Reverse();
                }

                return categories;
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar categorías", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar categorías", exception);
            }
        }

        public async Task<IReadOnlyList<CategoryModel>> GetCategoriesFromAnchorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long anchorCategoryId,
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                var hasAnchor = anchorCategoryId > 0;
                var orderByClause = BuildOrderByClause(sortBy, sortDirection);
                var hasSearchTerm = !string.IsNullOrWhiteSpace(ValidationText.NormalizeTrimmed(searchTerm));

                var queryBuilder = new StringBuilder();
                queryBuilder.Append("SELECT currentCategory.Id, currentCategory.Code, currentCategory.Name, currentCategory.Description, currentCategory.ProductCount FROM (");
                queryBuilder.Append(BuildCategoryRowsQuery(hasSearchTerm));
                queryBuilder.Append(") currentCategory");

                if (hasAnchor)
                {
                    queryBuilder.Append(" INNER JOIN (");
                    queryBuilder.Append(BuildCategoryRowsQuery(hasSearchTerm));
                    queryBuilder.Append(") anchorCategory ON anchorCategory.Id = @AnchorCategoryId WHERE ");
                    queryBuilder.Append(BuildAnchorInclusiveComparator(sortBy, sortDirection));
                }

                queryBuilder.Append(FormattableString.Invariant($@" ORDER BY {orderByClause}
                LIMIT @PageSize"));

                long? effectiveAnchorCategoryId = null;
                if (hasAnchor)
                {
                    effectiveAnchorCategoryId = anchorCategoryId;
                }

                var command = new CommandDefinition(
                queryBuilder.ToString(),
                parameters: BuildCategoryQueryParameters(searchTerm, pageSize, anchorCategoryId: effectiveAnchorCategoryId),
                cancellationToken: cancellationToken);

                var categories = await connection.QueryAsync<CategoryModel>(command);
                return categories.AsList();
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar categorías", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar categorías", exception);
            }
        }

        public async Task<bool> HasCategoriesByCursorAsync(
            string sortBy,
            string sortDirection,
            long cursorCategoryId,
            bool isNextPage,
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            if (cursorCategoryId <= 0)
            {
                return false;
            }

            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                var orderByClause = BuildOrderByClause(sortBy, sortDirection, reverse: !isNextPage);
                var keysetComparator = BuildKeysetComparator(sortBy, sortDirection, isNextPage);
                var hasSearchTerm = !string.IsNullOrWhiteSpace(ValidationText.NormalizeTrimmed(searchTerm));

                var queryBuilder = new StringBuilder();
                queryBuilder.Append("SELECT currentCategory.Id FROM (");
                queryBuilder.Append(BuildCategoryRowsQuery(hasSearchTerm));
                queryBuilder.Append(") currentCategory INNER JOIN (");
                queryBuilder.Append(BuildCategoryRowsQuery(hasSearchTerm));
                queryBuilder.Append(") cursorCategory ON cursorCategory.Id = @CursorCategoryId WHERE ");
                queryBuilder.Append(keysetComparator);
                queryBuilder.Append(FormattableString.Invariant($@" ORDER BY {orderByClause}
                LIMIT 1"));

            var command = new CommandDefinition(
                queryBuilder.ToString(),
                parameters: BuildCategoryQueryParameters(searchTerm, cursorCategoryId: cursorCategoryId),
                cancellationToken: cancellationToken);

                var nextCategoryId = await connection.QueryFirstOrDefaultAsync<long?>(command);
                return nextCategoryId.HasValue;
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar la navegación de categorías", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar la navegación de categorías", exception);
            }
        }

        public async Task<CategoryModel?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            try
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
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar la categoría", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar la categoría", exception);
            }
        }

        public async Task<long> CreateAsync(Category category, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(category);

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
                throw new BusinessValidationException("Code", "Ya existe una categoría con ese código.");
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                TryRollback(transaction);
                throw new BusinessValidationException("Los datos de la categoría no cumplen el formato requerido.");
            }
            catch (MySqlException exception)
            {
                TryRollback(transaction);
                throw CreateDataStoreUnavailableException("guardar la categoría", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                TryRollback(transaction);
                throw CreateDataStoreUnavailableException("guardar la categoría", exception);
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
            ArgumentNullException.ThrowIfNull(category);

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
                throw new BusinessValidationException("Code", "Ya existe una categoría con ese código.");
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                throw new BusinessValidationException("Los datos de la categoría no cumplen el formato requerido.");
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("actualizar la categoría", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("actualizar la categoría", exception);
            }
        }

        public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            try
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
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("eliminar la categoría", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("eliminar la categoría", exception);
            }
        }
    }
}
