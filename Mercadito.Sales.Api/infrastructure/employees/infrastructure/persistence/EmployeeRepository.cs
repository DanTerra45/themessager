using Mercadito.src.application.employees.ports.output;
using Dapper;
using Mercadito.src.shared.infrastructure.persistence;
using Mercadito.src.domain.employees.entities;
using Mercadito.src.application.employees.models;
using Mercadito.src.domain.shared.repository;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;
using Mercadito.src.domain.shared.exceptions;
using Mercadito.src.domain.shared.validation;

namespace Mercadito.src.infrastructure.employees.persistence
{
    public class EmployeeRepository(IDbConnectionFactory dbConnection) : IEmployeeRepository, ICrudRepository<Employee, Employee, EmployeeModel, long>
    {
        private const string ActiveState = "A";
        private const string InactiveState = "I";
        private const int ActiveUnique = 1;

        private readonly IDbConnectionFactory _dbConnection = dbConnection;

        private static DataStoreUnavailableException CreateDataStoreUnavailableException(string operation, Exception exception)
        {
            return new DataStoreUnavailableException($"No se pudo {operation} porque la base de datos no está disponible.", exception);
        }

        private enum SortFieldDirection
        {
            Primary,
            Asc
        }

        private readonly struct SortField(string column, SortFieldDirection direction)
        {
            public string Column { get; } = column;
            public SortFieldDirection Direction { get; } = direction;
        }

        private static string BuildEmployeeRowsQuery(bool hasSearchTerm)
        {
            var queryBuilder = new StringBuilder(@"SELECT
                    id AS Id,
                    ci AS Ci,
                    COALESCE(complemento, '') AS Complemento,
                    nombres AS Nombres,
                    primerApellido AS PrimerApellido,
                    COALESCE(segundoApellido, '') AS SegundoApellido,
                    cargo AS Cargo,
                    numeroContacto AS NumeroContacto
                    FROM empleados
                    WHERE estado = @ActiveState");

            if (hasSearchTerm)
            {
                queryBuilder.Append(@"
                    AND (
                        CAST(ci AS CHAR) LIKE @SearchPattern
                        OR COALESCE(complemento, '') LIKE @SearchPattern
                        OR CONCAT(CAST(ci AS CHAR), IF(COALESCE(complemento, '') = '', '', CONCAT('-', complemento))) LIKE @SearchPattern
                        OR nombres LIKE @SearchPattern
                        OR primerApellido LIKE @SearchPattern
                        OR COALESCE(segundoApellido, '') LIKE @SearchPattern
                        OR TRIM(CONCAT(primerApellido, ' ', COALESCE(segundoApellido, ''), ' ', nombres)) LIKE @SearchPattern
                        OR TRIM(CONCAT(nombres, ' ', primerApellido, ' ', COALESCE(segundoApellido, ''))) LIKE @SearchPattern
                        OR numeroContacto LIKE @SearchPattern
                        OR cargo LIKE @SearchPattern
                    )");
            }

            return queryBuilder.ToString();
        }

        private static DynamicParameters BuildEmployeeQueryParameters(string searchTerm, int? pageSize = null, long? cursorEmployeeId = null, long? anchorEmployeeId = null)
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

            if (cursorEmployeeId.HasValue)
            {
                parameters.Add("CursorEmployeeId", cursorEmployeeId.Value);
            }

            if (anchorEmployeeId.HasValue)
            {
                parameters.Add("AnchorEmployeeId", anchorEmployeeId.Value);
            }

            return parameters;
        }

        private static SortField[] GetSortFields(string normalizedSortBy)
        {
            return normalizedSortBy switch
            {
                "id" => [new SortField("Id", SortFieldDirection.Primary)],
                "ci" => [new SortField("Ci", SortFieldDirection.Primary)],
                "nombres" => [new SortField("Nombres", SortFieldDirection.Primary), new SortField("PrimerApellido", SortFieldDirection.Primary)],
                "cargo" => [new SortField("Cargo", SortFieldDirection.Primary), new SortField("PrimerApellido", SortFieldDirection.Asc), new SortField("Nombres", SortFieldDirection.Asc)],
                _ => [new SortField("PrimerApellido", SortFieldDirection.Primary), new SortField("SegundoApellido", SortFieldDirection.Primary), new SortField("Nombres", SortFieldDirection.Primary)]
            };
        }

        private static string ResolveFieldDirection(SortField field, string primaryDirection, bool reverse)
        {
            var direction = "ASC";
            if (field.Direction == SortFieldDirection.Primary)
            {
                direction = primaryDirection;
            }
            if (!reverse)
            {
                return direction;
            }

            if (string.Equals(direction, "ASC", StringComparison.Ordinal))
            {
                return "DESC";
            }

            return "ASC";
        }

        private static string ResolveNavigationComparator(string fieldDirection, bool isNextPage)
        {
            var isAscending = string.Equals(fieldDirection, "ASC", StringComparison.Ordinal);
            if (isNextPage)
            {
                if (isAscending)
                {
                    return ">";
                }

                return "<";
            }

            if (isAscending)
            {
                return "<";
            }

            return ">";
        }

        private static string ResolveAnchorComparator(string fieldDirection)
        {
            if (string.Equals(fieldDirection, "ASC", StringComparison.Ordinal))
            {
                return ">";
            }

            return "<";
        }

        private static string BuildOrderByClause(string sortBy, string sortDirection, bool reverse = false)
        {
            var normalizedSortBy = NormalizeSortBy(sortBy);
            var primaryDirection = NormalizeSortDirection(sortDirection);
            var sortFields = GetSortFields(normalizedSortBy);

            var orderSegments = new List<string>(sortFields.Length + 1);
            var includesIdColumn = false;

            foreach (var sortField in sortFields)
            {
                var fieldDirection = ResolveFieldDirection(sortField, primaryDirection, reverse);
                orderSegments.Add($"currentEmployee.{sortField.Column} {fieldDirection}");
                if (string.Equals(sortField.Column, "Id", StringComparison.Ordinal))
                {
                    includesIdColumn = true;
                }
            }

            if (!includesIdColumn)
            {
                var idDirection = "ASC";
                if (reverse)
                {
                    idDirection = "DESC";
                }

                orderSegments.Add($"currentEmployee.Id {idDirection}");
            }

            return string.Join(", ", orderSegments);
        }

        private static string BuildKeysetComparator(string sortBy, string sortDirection, bool isNextPage)
        {
            var normalizedSortBy = NormalizeSortBy(sortBy);
            var primaryDirection = NormalizeSortDirection(sortDirection);

            if (string.Equals(normalizedSortBy, "id", StringComparison.Ordinal))
            {
                var isAscending = string.Equals(primaryDirection, "ASC", StringComparison.Ordinal);
                if (isNextPage)
                {
                    if (isAscending)
                    {
                        return "currentEmployee.Id > cursorEmployee.Id";
                    }

                    return "currentEmployee.Id < cursorEmployee.Id";
                }

                if (isAscending)
                {
                    return "currentEmployee.Id < cursorEmployee.Id";
                }

                return "currentEmployee.Id > cursorEmployee.Id";
            }

            var sortFields = GetSortFields(normalizedSortBy);
            var conditions = new List<string>(sortFields.Length + 1);
            var equalityPrefixBuilder = new StringBuilder();

            for (var index = 0; index < sortFields.Length; index++)
            {
                var sortField = sortFields[index];
                var fieldDirection = ResolveFieldDirection(sortField, primaryDirection, reverse: false);
                var comparator = ResolveNavigationComparator(fieldDirection, isNextPage);

                if (index == 0)
                {
                    conditions.Add($"currentEmployee.{sortField.Column} {comparator} cursorEmployee.{sortField.Column}");
                    equalityPrefixBuilder.Append(FormattableString.Invariant($"currentEmployee.{sortField.Column} = cursorEmployee.{sortField.Column}"));
                    continue;
                }

                conditions.Add($"({equalityPrefixBuilder} AND currentEmployee.{sortField.Column} {comparator} cursorEmployee.{sortField.Column})");
                equalityPrefixBuilder.Append(FormattableString.Invariant($" AND currentEmployee.{sortField.Column} = cursorEmployee.{sortField.Column}"));
            }

            var idComparator = "<";
            if (isNextPage)
            {
                idComparator = ">";
            }
            conditions.Add($"({equalityPrefixBuilder} AND currentEmployee.Id {idComparator} cursorEmployee.Id)");

            return $"({string.Join(" OR ", conditions)})";
        }

        private static string BuildAnchorInclusiveComparator(string sortBy, string sortDirection)
        {
            var normalizedSortBy = NormalizeSortBy(sortBy);
            var primaryDirection = NormalizeSortDirection(sortDirection);

            if (string.Equals(normalizedSortBy, "id", StringComparison.Ordinal))
            {
                if (string.Equals(primaryDirection, "ASC", StringComparison.Ordinal))
                {
                    return "currentEmployee.Id >= anchorEmployee.Id";
                }

                return "currentEmployee.Id <= anchorEmployee.Id";
            }

            var sortFields = GetSortFields(normalizedSortBy);
            var conditions = new List<string>(sortFields.Length + 1);
            var equalityPrefixBuilder = new StringBuilder();

            for (var index = 0; index < sortFields.Length; index++)
            {
                var sortField = sortFields[index];
                var fieldDirection = ResolveFieldDirection(sortField, primaryDirection, reverse: false);
                var comparator = ResolveAnchorComparator(fieldDirection);

                if (index == 0)
                {
                    conditions.Add($"currentEmployee.{sortField.Column} {comparator} anchorEmployee.{sortField.Column}");
                    equalityPrefixBuilder.Append(FormattableString.Invariant($"currentEmployee.{sortField.Column} = anchorEmployee.{sortField.Column}"));
                    continue;
                }

                conditions.Add($"({equalityPrefixBuilder} AND currentEmployee.{sortField.Column} {comparator} anchorEmployee.{sortField.Column})");
                equalityPrefixBuilder.Append(FormattableString.Invariant($" AND currentEmployee.{sortField.Column} = anchorEmployee.{sortField.Column}"));
            }

            conditions.Add($"({equalityPrefixBuilder} AND currentEmployee.Id >= anchorEmployee.Id)");
            return $"({string.Join(" OR ", conditions)})";
        }

        private static string NormalizeSortBy(string sortBy)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return "apellidos";
            }

            var normalizedSortBy = ValidationText.NormalizeLowerTrimmed(sortBy);
            return normalizedSortBy switch
            {
                "id" => "id",
                "ci" => "ci",
                "nombres" => "nombres",
                "rol" => "cargo",
                "cargo" => "cargo",
                _ => "apellidos"
            };
        }

        private static string NormalizeSortDirection(string sortDirection)
        {
            if (string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            {
                return "DESC";
            }

            return "ASC";
        }

        private static CommandDefinition BuildActiveEmployeeIdentityExistsCommand(
            long ci,
            string normalizedComplemento,
            bool excludeCurrentEmployee,
            long currentEmployeeId,
            CancellationToken cancellationToken)
        {
            var queryBuilder = new StringBuilder(@"SELECT EXISTS(
                    SELECT 1
                    FROM empleados
                    WHERE ci = @Ci
                    AND complementoNormalizado = @Complemento
                    AND activoUnico = @ActiveUnique");

            var parameters = new DynamicParameters();
            parameters.Add("Ci", ci);
            parameters.Add("Complemento", normalizedComplemento);
            parameters.Add("ActiveUnique", ActiveUnique);

            if (excludeCurrentEmployee)
            {
                queryBuilder.Append(" AND id <> @CurrentEmployeeId");
                parameters.Add("CurrentEmployeeId", currentEmployeeId);
            }

            queryBuilder.Append(" LIMIT 1)");

            return new CommandDefinition(
                queryBuilder.ToString(),
                parameters: parameters,
                cancellationToken: cancellationToken);
        }

        private static async Task EnsureUniqueActiveIdentityAsync(
            IDbConnection connection,
            Employee employee,
            bool excludeCurrentEmployee,
            long currentEmployeeId,
            CancellationToken cancellationToken)
        {
            var normalizedComplemento = ValidationText.NormalizeUpperTrimmed(employee.Complemento);
            var existsCommand = BuildActiveEmployeeIdentityExistsCommand(
                employee.Ci,
                normalizedComplemento,
                excludeCurrentEmployee,
                currentEmployeeId,
                cancellationToken);

            var identityAlreadyExists = await connection.ExecuteScalarAsync<bool>(existsCommand);
            if (identityAlreadyExists)
            {
                throw new ValidationException("Ya existe un empleado activo con el mismo CI y complemento.");
            }
        }

        public async Task<IReadOnlyList<EmployeeModel>> GetEmployeesByCursorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorEmployeeId,
            bool isNextPage,
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            if (cursorEmployeeId <= 0)
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
                queryBuilder.Append("SELECT currentEmployee.Id, currentEmployee.Ci, currentEmployee.Complemento, currentEmployee.Nombres, currentEmployee.PrimerApellido, currentEmployee.SegundoApellido, currentEmployee.Cargo, currentEmployee.NumeroContacto FROM (");
                queryBuilder.Append(BuildEmployeeRowsQuery(hasSearchTerm));
                queryBuilder.Append(") currentEmployee INNER JOIN (");
                queryBuilder.Append(BuildEmployeeRowsQuery(hasSearchTerm));
                queryBuilder.Append(") cursorEmployee ON cursorEmployee.Id = @CursorEmployeeId WHERE ");
                queryBuilder.Append(keysetComparator);
                queryBuilder.Append(FormattableString.Invariant($@" ORDER BY {orderByClause}
                    LIMIT @PageSize"));

                var command = new CommandDefinition(
                queryBuilder.ToString(),
                parameters: BuildEmployeeQueryParameters(searchTerm, pageSize, cursorEmployeeId: cursorEmployeeId),
                cancellationToken: cancellationToken);

                var employees = (await connection.QueryAsync<EmployeeModel>(command)).AsList();
                if (!isNextPage)
                {
                    employees.Reverse();
                }

                return employees;
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar empleados", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar empleados", exception);
            }
        }

        public async Task<IReadOnlyList<EmployeeModel>> GetEmployeesFromAnchorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long anchorEmployeeId,
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                var hasAnchor = anchorEmployeeId > 0;
                var orderByClause = BuildOrderByClause(sortBy, sortDirection);
                var hasSearchTerm = !string.IsNullOrWhiteSpace(ValidationText.NormalizeTrimmed(searchTerm));

                var queryBuilder = new StringBuilder();
                queryBuilder.Append("SELECT currentEmployee.Id, currentEmployee.Ci, currentEmployee.Complemento, currentEmployee.Nombres, currentEmployee.PrimerApellido, currentEmployee.SegundoApellido, currentEmployee.Cargo, currentEmployee.NumeroContacto FROM (");
                queryBuilder.Append(BuildEmployeeRowsQuery(hasSearchTerm));
                queryBuilder.Append(") currentEmployee");

                if (hasAnchor)
                {
                    queryBuilder.Append(" INNER JOIN (");
                    queryBuilder.Append(BuildEmployeeRowsQuery(hasSearchTerm));
                    queryBuilder.Append(") anchorEmployee ON anchorEmployee.Id = @AnchorEmployeeId WHERE ");
                    queryBuilder.Append(BuildAnchorInclusiveComparator(sortBy, sortDirection));
                }

                queryBuilder.Append(FormattableString.Invariant($@" ORDER BY {orderByClause}
                    LIMIT @PageSize"));

                long? effectiveAnchorEmployeeId = null;
                if (hasAnchor)
                {
                    effectiveAnchorEmployeeId = anchorEmployeeId;
                }

                var command = new CommandDefinition(
                queryBuilder.ToString(),
                parameters: BuildEmployeeQueryParameters(searchTerm, pageSize, anchorEmployeeId: effectiveAnchorEmployeeId),
                cancellationToken: cancellationToken);

                var employees = await connection.QueryAsync<EmployeeModel>(command);
                return employees.AsList();
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar empleados", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar empleados", exception);
            }
        }

        public async Task<bool> HasEmployeesByCursorAsync(
            string sortBy,
            string sortDirection,
            long cursorEmployeeId,
            bool isNextPage,
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            if (cursorEmployeeId <= 0)
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
                queryBuilder.Append("SELECT currentEmployee.Id FROM (");
                queryBuilder.Append(BuildEmployeeRowsQuery(hasSearchTerm));
                queryBuilder.Append(") currentEmployee INNER JOIN (");
                queryBuilder.Append(BuildEmployeeRowsQuery(hasSearchTerm));
                queryBuilder.Append(") cursorEmployee ON cursorEmployee.Id = @CursorEmployeeId WHERE ");
                queryBuilder.Append(keysetComparator);
                queryBuilder.Append(FormattableString.Invariant($@" ORDER BY {orderByClause}
                    LIMIT 1"));

            var command = new CommandDefinition(
                queryBuilder.ToString(),
                parameters: BuildEmployeeQueryParameters(searchTerm, cursorEmployeeId: cursorEmployeeId),
                cancellationToken: cancellationToken);

                var nextEmployeeId = await connection.QueryFirstOrDefaultAsync<long?>(command);
                return nextEmployeeId.HasValue;
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar la navegación de empleados", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar la navegación de empleados", exception);
            }
        }

        public async Task<EmployeeModel?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                const string query = @"SELECT
                    id AS Id,
                    ci AS Ci,
                    complemento AS Complemento,
                    nombres AS Nombres,
                    primerApellido AS PrimerApellido,
                    segundoApellido AS SegundoApellido,
                    cargo AS Cargo,
                    numeroContacto AS NumeroContacto
                    FROM empleados
                    WHERE id = @Id AND estado = @ActiveState";

                var command = new CommandDefinition(
                query,
                parameters: new { Id = id, ActiveState },
                cancellationToken: cancellationToken);

                return await connection.QueryFirstOrDefaultAsync<EmployeeModel>(command);
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar el empleado", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar el empleado", exception);
            }
        }

        public async Task<long> CreateAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(employee);

            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            await EnsureUniqueActiveIdentityAsync(
                connection,
                employee,
                excludeCurrentEmployee: false,
                currentEmployeeId: 0,
                cancellationToken);

            try
            {
                const string query = @"INSERT INTO empleados
                        (ci, complemento, nombres, primerApellido, segundoApellido, cargo, numeroContacto, estado)
                        VALUES
                        (@Ci, @Complemento, @Nombres, @PrimerApellido, @SegundoApellido, @Cargo, @NumeroContacto, @ActiveState);
                        SELECT LAST_INSERT_ID();";

                var insertEmployeeCommand = new CommandDefinition(
                    query,
                    parameters: new
                    {
                        employee.Ci,
                        employee.Complemento,
                        employee.Nombres,
                        employee.PrimerApellido,
                        employee.SegundoApellido,
                        employee.Cargo,
                        employee.NumeroContacto,
                        ActiveState
                    },
                    cancellationToken: cancellationToken);

                return await connection.ExecuteScalarAsync<long>(insertEmployeeCommand);
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                throw new BusinessValidationException(new Dictionary<string, List<string>>
                {
                    ["Ci"] = ["Ya existe un empleado activo con el mismo CI y complemento."],
                    ["Complemento"] = ["Ya existe un empleado activo con el mismo CI y complemento."]
                });
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                throw new BusinessValidationException("Los datos del empleado no cumplen el formato requerido.");
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("guardar el empleado", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("guardar el empleado", exception);
            }
        }

        public async Task<int> UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(employee);

            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            await EnsureUniqueActiveIdentityAsync(
                connection,
                employee,
                excludeCurrentEmployee: true,
                currentEmployeeId: employee.Id,
                cancellationToken);

            try
            {
                const string query = @"UPDATE empleados SET
                        ci = @Ci,
                        complemento = @Complemento,
                        nombres = @Nombres,
                        primerApellido = @PrimerApellido,
                        segundoApellido = @SegundoApellido,
                        cargo = @Cargo,
                        numeroContacto = @NumeroContacto
                        WHERE id = @Id AND estado = @ActiveState";

                var command = new CommandDefinition(
                    query,
                    parameters: new
                    {
                        employee.Id,
                        employee.Ci,
                        employee.Complemento,
                        employee.Nombres,
                        employee.PrimerApellido,
                        employee.SegundoApellido,
                        employee.Cargo,
                        employee.NumeroContacto,
                        ActiveState
                    },
                    cancellationToken: cancellationToken);

                return await connection.ExecuteAsync(command);
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                throw new BusinessValidationException(new Dictionary<string, List<string>>
                {
                    ["Ci"] = ["Ya existe un empleado activo con el mismo CI y complemento."],
                    ["Complemento"] = ["Ya existe un empleado activo con el mismo CI y complemento."]
                });
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                throw new BusinessValidationException("Los datos del empleado no cumplen el formato requerido.");
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("actualizar el empleado", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("actualizar el empleado", exception);
            }
        }

        public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                const string query = "UPDATE empleados SET estado = @InactiveState WHERE id = @Id AND estado = @ActiveState";

                var command = new CommandDefinition(
                query,
                parameters: new { Id = id, ActiveState, InactiveState },
                cancellationToken: cancellationToken);

                return await connection.ExecuteAsync(command);
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("eliminar el empleado", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("eliminar el empleado", exception);
            }
        }
    }
}
