using Dapper;
using Mercadito.database.interfaces;
using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.model;
using Mercadito.src.shared.domain.repository;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;

namespace Mercadito.src.employees.data.repository
{
    public class EmployeeRepository(IDbConnectionFactory dbConnection) : ICrudRepository<Employee, Employee, EmployeeModel, long>
    {
        private const string ActiveState = "A";
        private const string InactiveState = "I";
        private const int ActiveUnique = 1;

        private readonly IDbConnectionFactory _dbConnection = dbConnection;

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

        private static string BuildEmployeeRowsQuery()
        {
            return @"SELECT
                    id AS Id,
                    ci AS Ci,
                    COALESCE(complemento, '') AS Complemento,
                    nombres AS Nombres,
                    primerApellido AS PrimerApellido,
                    COALESCE(segundoApellido, '') AS SegundoApellido,
                    rol AS Rol,
                    numeroContacto AS NumeroContacto
                    FROM empleados
                    WHERE estado = @ActiveState";
        }

        private static SortField[] GetSortFields(string normalizedSortBy)
        {
            return normalizedSortBy switch
            {
                "id" => [new SortField("Id", SortFieldDirection.Primary)],
                "ci" => [new SortField("Ci", SortFieldDirection.Primary)],
                "nombres" => [new SortField("Nombres", SortFieldDirection.Primary), new SortField("PrimerApellido", SortFieldDirection.Primary)],
                "rol" => [new SortField("Rol", SortFieldDirection.Primary), new SortField("PrimerApellido", SortFieldDirection.Asc), new SortField("Nombres", SortFieldDirection.Asc)],
                _ => [new SortField("PrimerApellido", SortFieldDirection.Primary), new SortField("SegundoApellido", SortFieldDirection.Primary), new SortField("Nombres", SortFieldDirection.Primary)]
            };
        }

        private static string ResolveFieldDirection(SortField field, string primaryDirection, bool reverse)
        {
            var direction = field.Direction == SortFieldDirection.Primary ? primaryDirection : "ASC";
            if (!reverse)
            {
                return direction;
            }

            return string.Equals(direction, "ASC", StringComparison.Ordinal)
                ? "DESC"
                : "ASC";
        }

        private static string ResolveNavigationComparator(string fieldDirection, bool isNextPage)
        {
            var isAscending = string.Equals(fieldDirection, "ASC", StringComparison.Ordinal);
            if (isNextPage)
            {
                return isAscending ? ">" : "<";
            }

            return isAscending ? "<" : ">";
        }

        private static string ResolveAnchorComparator(string fieldDirection)
        {
            return string.Equals(fieldDirection, "ASC", StringComparison.Ordinal) ? ">" : "<";
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
                orderSegments.Add($"currentEmployee.Id {(reverse ? "DESC" : "ASC")}");
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
                    return isAscending ? "currentEmployee.Id > cursorEmployee.Id" : "currentEmployee.Id < cursorEmployee.Id";
                }

                return isAscending ? "currentEmployee.Id < cursorEmployee.Id" : "currentEmployee.Id > cursorEmployee.Id";
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
                    equalityPrefixBuilder.Append($"currentEmployee.{sortField.Column} = cursorEmployee.{sortField.Column}");
                    continue;
                }

                conditions.Add($"({equalityPrefixBuilder} AND currentEmployee.{sortField.Column} {comparator} cursorEmployee.{sortField.Column})");
                equalityPrefixBuilder.Append($" AND currentEmployee.{sortField.Column} = cursorEmployee.{sortField.Column}");
            }

            var idComparator = isNextPage ? ">" : "<";
            conditions.Add($"({equalityPrefixBuilder} AND currentEmployee.Id {idComparator} cursorEmployee.Id)");

            return $"({string.Join(" OR ", conditions)})";
        }

        private static string BuildAnchorInclusiveComparator(string sortBy, string sortDirection)
        {
            var normalizedSortBy = NormalizeSortBy(sortBy);
            var primaryDirection = NormalizeSortDirection(sortDirection);

            if (string.Equals(normalizedSortBy, "id", StringComparison.Ordinal))
            {
                return string.Equals(primaryDirection, "ASC", StringComparison.Ordinal)
                    ? "currentEmployee.Id >= anchorEmployee.Id"
                    : "currentEmployee.Id <= anchorEmployee.Id";
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
                    equalityPrefixBuilder.Append($"currentEmployee.{sortField.Column} = anchorEmployee.{sortField.Column}");
                    continue;
                }

                conditions.Add($"({equalityPrefixBuilder} AND currentEmployee.{sortField.Column} {comparator} anchorEmployee.{sortField.Column})");
                equalityPrefixBuilder.Append($" AND currentEmployee.{sortField.Column} = anchorEmployee.{sortField.Column}");
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

            var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
            return normalizedSortBy switch
            {
                "id" => "id",
                "ci" => "ci",
                "nombres" => "nombres",
                "rol" => "rol",
                _ => "apellidos"
            };
        }

        private static string NormalizeSortDirection(string sortDirection)
        {
            return string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        }

        private static string NormalizeComplemento(string? complemento)
        {
            if (string.IsNullOrWhiteSpace(complemento))
            {
                return string.Empty;
            }

            return complemento.Trim().ToUpperInvariant();
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
            var normalizedComplemento = NormalizeComplemento(employee.Complemento);
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
            CancellationToken cancellationToken = default)
        {
            if (cursorEmployeeId <= 0)
            {
                return [];
            }

            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            var orderByClause = BuildOrderByClause(sortBy, sortDirection, reverse: !isNextPage);
            var keysetComparator = BuildKeysetComparator(sortBy, sortDirection, isNextPage);

            var queryBuilder = new StringBuilder();
            queryBuilder.Append("SELECT currentEmployee.Id, currentEmployee.Ci, currentEmployee.Complemento, currentEmployee.Nombres, currentEmployee.PrimerApellido, currentEmployee.SegundoApellido, currentEmployee.Rol, currentEmployee.NumeroContacto FROM (");
            queryBuilder.Append(BuildEmployeeRowsQuery());
            queryBuilder.Append(") currentEmployee INNER JOIN (");
            queryBuilder.Append(BuildEmployeeRowsQuery());
            queryBuilder.Append(") cursorEmployee ON cursorEmployee.Id = @CursorEmployeeId WHERE ");
            queryBuilder.Append(keysetComparator);
            queryBuilder.Append($@" ORDER BY {orderByClause}
                    LIMIT @PageSize");

            var command = new CommandDefinition(
                queryBuilder.ToString(),
                parameters: new
                {
                    ActiveState,
                    CursorEmployeeId = cursorEmployeeId,
                    PageSize = pageSize
                },
                cancellationToken: cancellationToken);

            var employees = (await connection.QueryAsync<EmployeeModel>(command)).AsList();
            if (!isNextPage)
            {
                employees.Reverse();
            }

            return employees;
        }

        public async Task<IReadOnlyList<EmployeeModel>> GetEmployeesFromAnchorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long anchorEmployeeId,
            CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            var hasAnchor = anchorEmployeeId > 0;
            var orderByClause = BuildOrderByClause(sortBy, sortDirection);

            var queryBuilder = new StringBuilder();
            queryBuilder.Append("SELECT currentEmployee.Id, currentEmployee.Ci, currentEmployee.Complemento, currentEmployee.Nombres, currentEmployee.PrimerApellido, currentEmployee.SegundoApellido, currentEmployee.Rol, currentEmployee.NumeroContacto FROM (");
            queryBuilder.Append(BuildEmployeeRowsQuery());
            queryBuilder.Append(") currentEmployee");

            if (hasAnchor)
            {
                queryBuilder.Append(" INNER JOIN (");
                queryBuilder.Append(BuildEmployeeRowsQuery());
                queryBuilder.Append(") anchorEmployee ON anchorEmployee.Id = @AnchorEmployeeId WHERE ");
                queryBuilder.Append(BuildAnchorInclusiveComparator(sortBy, sortDirection));
            }

            queryBuilder.Append($@" ORDER BY {orderByClause}
                    LIMIT @PageSize");

            var parameters = new DynamicParameters();
            parameters.Add("ActiveState", ActiveState);
            parameters.Add("PageSize", pageSize);
            if (hasAnchor)
            {
                parameters.Add("AnchorEmployeeId", anchorEmployeeId);
            }

            var command = new CommandDefinition(
                queryBuilder.ToString(),
                parameters: parameters,
                cancellationToken: cancellationToken);

            var employees = await connection.QueryAsync<EmployeeModel>(command);
            return employees.AsList();
        }

        public async Task<bool> HasEmployeesByCursorAsync(
            string sortBy,
            string sortDirection,
            long cursorEmployeeId,
            bool isNextPage,
            CancellationToken cancellationToken = default)
        {
            if (cursorEmployeeId <= 0)
            {
                return false;
            }

            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            var orderByClause = BuildOrderByClause(sortBy, sortDirection, reverse: !isNextPage);
            var keysetComparator = BuildKeysetComparator(sortBy, sortDirection, isNextPage);

            var queryBuilder = new StringBuilder();
            queryBuilder.Append("SELECT currentEmployee.Id FROM (");
            queryBuilder.Append(BuildEmployeeRowsQuery());
            queryBuilder.Append(") currentEmployee INNER JOIN (");
            queryBuilder.Append(BuildEmployeeRowsQuery());
            queryBuilder.Append(") cursorEmployee ON cursorEmployee.Id = @CursorEmployeeId WHERE ");
            queryBuilder.Append(keysetComparator);
            queryBuilder.Append($@" ORDER BY {orderByClause}
                    LIMIT 1");

            var command = new CommandDefinition(
                queryBuilder.ToString(),
                parameters: new
                {
                    ActiveState,
                    CursorEmployeeId = cursorEmployeeId
                },
                cancellationToken: cancellationToken);

            var nextEmployeeId = await connection.QueryFirstOrDefaultAsync<long?>(command);
            return nextEmployeeId.HasValue;
        }

        public async Task<EmployeeModel?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"SELECT
                    id AS Id,
                    ci AS Ci,
                    complemento AS Complemento,
                    nombres AS Nombres,
                    primerApellido AS PrimerApellido,
                    segundoApellido AS SegundoApellido,
                    rol AS Rol,
                    numeroContacto AS NumeroContacto
                    FROM empleados
                    WHERE id = @Id AND estado = @ActiveState";

            var command = new CommandDefinition(
                query,
                parameters: new { Id = id, ActiveState },
                cancellationToken: cancellationToken);

            return await connection.QueryFirstOrDefaultAsync<EmployeeModel>(command);
        }

        public async Task<long> CreateAsync(Employee employee, CancellationToken cancellationToken = default)
        {
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
                        (ci, complemento, nombres, primerApellido, segundoApellido, rol, numeroContacto, estado)
                        VALUES
                        (@Ci, @Complemento, @Nombres, @PrimerApellido, @SegundoApellido, @Rol, @NumeroContacto, @ActiveState);
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
                        employee.Rol,
                        employee.NumeroContacto,
                        ActiveState
                    },
                    cancellationToken: cancellationToken);

                return await connection.ExecuteScalarAsync<long>(insertEmployeeCommand);
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                throw new ValidationException("Ya existe un empleado activo con el mismo CI y complemento.");
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                throw new ValidationException("Los datos del empleado no cumplen el formato requerido.");
            }
        }

        public async Task<int> UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
        {
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
                        rol = @Rol,
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
                        employee.Rol,
                        employee.NumeroContacto,
                        ActiveState
                    },
                    cancellationToken: cancellationToken);

                return await connection.ExecuteAsync(command);
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                throw new ValidationException("Ya existe un empleado activo con el mismo CI y complemento.");
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                throw new ValidationException("Los datos del empleado no cumplen el formato requerido.");
            }
        }

        public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = "UPDATE empleados SET estado = @InactiveState WHERE id = @Id AND estado = @ActiveState";

            var command = new CommandDefinition(
                query,
                parameters: new { Id = id, ActiveState, InactiveState },
                cancellationToken: cancellationToken);

            return await connection.ExecuteAsync(command);
        }
    }
}
