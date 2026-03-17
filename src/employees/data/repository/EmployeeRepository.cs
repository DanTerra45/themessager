using Dapper;
using Mercadito.database.interfaces;
using Mercadito.src.employees.data.entity;
using Mercadito.src.shared.domain.repository;
using MySqlConnector;
using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Mercadito.src.employees.data.repository
{
    public class EmployeeRepository(IDbConnectionFactory dbConnection) : ICrudRepository<Employee, Employee, Employee, long>
    {
        private const string ActiveState = "A";
        private const string InactiveState = "I";

        private readonly IDbConnectionFactory _dbConnection = dbConnection;

        private static string NormalizeComplemento(string? complemento)
        {
            if (string.IsNullOrWhiteSpace(complemento))
            {
                return string.Empty;
            }

            return complemento.Trim().ToUpperInvariant();
        }

        private static CommandDefinition BuildCountActiveEmployeesByIdentityCommand(
            long ci,
            string normalizedComplemento,
            bool excludeCurrentEmployee,
            long currentEmployeeId,
            CancellationToken cancellationToken)
        {
            var queryBuilder = new StringBuilder(@"SELECT COUNT(*)
                    FROM empleados
                    WHERE ci = @Ci
                    AND COALESCE(complemento, '') = @Complemento
                    AND estado = @ActiveState");

            var parameters = new DynamicParameters();
            parameters.Add("Ci", ci);
            parameters.Add("Complemento", normalizedComplemento);
            parameters.Add("ActiveState", ActiveState);

            if (excludeCurrentEmployee)
            {
                queryBuilder.Append(" AND id <> @CurrentEmployeeId");
                parameters.Add("CurrentEmployeeId", currentEmployeeId);
            }

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
            var countCommand = BuildCountActiveEmployeesByIdentityCommand(
                employee.Ci,
                normalizedComplemento,
                excludeCurrentEmployee,
                currentEmployeeId,
                cancellationToken);

            var existingEmployeesCount = await connection.ExecuteScalarAsync<int>(countCommand);
            if (existingEmployeesCount > 0)
            {
                throw new ValidationException("Ya existe un empleado activo con el mismo CI y complemento.");
            }
        }

        public async Task<IReadOnlyList<Employee>> GetEmployeesByPages(
            int page,
            int pageSize,
            string sortBy,
            string sortDirection,
            CancellationToken cancellationToken = default)
        {
            var offset = (page - 1) * pageSize;
            var orderByClause = BuildOrderByClause(sortBy, sortDirection);
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            var query = $@"SELECT
                    id AS Id,
                    ci AS Ci,
                    complemento AS Complemento,
                    nombres AS Nombres,
                    primerApellido AS PrimerApellido,
                    segundoApellido AS SegundoApellido,
                    rol AS Rol,
                    numeroContacto AS NumeroContacto
                    FROM empleados
                    WHERE estado = @ActiveState
                    ORDER BY {orderByClause}
                    LIMIT @PageSize OFFSET @Offset";

            var command = new CommandDefinition(
                query,
                parameters: new { ActiveState, Offset = offset, PageSize = pageSize },
                cancellationToken: cancellationToken);

            var employees = await connection.QueryAsync<Employee>(command);
            return employees.AsList();
        }

        public async Task<int> GetTotalEmployeesCountAsync(CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = "SELECT COUNT(*) FROM empleados WHERE estado = @ActiveState";

            var command = new CommandDefinition(query, parameters: new { ActiveState }, cancellationToken: cancellationToken);
            return await connection.ExecuteScalarAsync<int>(command);
        }

        public async Task<Employee?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
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

            return await connection.QueryFirstOrDefaultAsync<Employee>(command);
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

        private static string BuildOrderByClause(string sortBy, string sortDirection)
        {
            var direction = NormalizeSortDirection(sortDirection);
            var normalizedSortBy = string.IsNullOrWhiteSpace(sortBy)
                ? "apellidos"
                : sortBy.Trim().ToLowerInvariant();

            return normalizedSortBy switch
            {
                "id" => $"id {direction}",
                "ci" => $"ci {direction}, id ASC",
                "nombres" => $"nombres {direction}, primerApellido {direction}, id ASC",
                "rol" => $"rol {direction}, primerApellido ASC, nombres ASC, id ASC",
                _ => $"primerApellido {direction}, segundoApellido {direction}, nombres {direction}, id ASC"
            };
        }

        private static string NormalizeSortDirection(string sortDirection)
        {
            return string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        }
    }
}
