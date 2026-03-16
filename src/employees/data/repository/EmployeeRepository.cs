using Dapper;
using Mercadito.database.interfaces;
using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.repository;

namespace Mercadito.src.employees.data.repository
{
    public class EmployeeRepository(IDataBaseConnection dbConnection) : IEmployeeRepository
    {
        private const string ActiveState = "A";
        private const string InactiveState = "I";

        private readonly IDataBaseConnection _dbConnection = dbConnection;

        public async Task<IReadOnlyList<Employee>> GetEmployeesByPages(int page, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var offset = (page - 1) * pageSize;
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"SELECT
                    id AS Id,
                    ci AS Ci,
                    complemento AS Complemento,
                    nombres AS Nombres,
                    primerApellido AS PrimerApellido,
                    segundoApellido AS SegundoApellido,
                    rol AS Rol,
                    numeroContacto AS NumeroContacto,
                    (estado = @ActiveState) AS IsActive
                    FROM empleados
                    WHERE estado = @ActiveState
                    ORDER BY primerApellido, segundoApellido, nombres
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

        public async Task<Employee?> GetEmployeeByIdAsync(long id, CancellationToken cancellationToken = default)
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
                    numeroContacto AS NumeroContacto,
                    (estado = @ActiveState) AS IsActive
                    FROM empleados
                    WHERE id = @Id AND estado = @ActiveState";

            var command = new CommandDefinition(
                query,
                parameters: new { Id = id, ActiveState },
                cancellationToken: cancellationToken);

            return await connection.QueryFirstOrDefaultAsync<Employee>(command);
        }

        public async Task<long> AddEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
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

        public async Task<int> UpdateEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"UPDATE empleados SET
                    ci = @Ci,
                    complemento = @Complemento,
                    nombres = @Nombres,
                    primerApellido = @PrimerApellido,
                    segundoApellido = @SegundoApellido,
                    rol = @Rol,
                    numeroContacto = @NumeroContacto,
                    estado = CASE WHEN @IsActive THEN @ActiveState ELSE @InactiveState END
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
                    employee.IsActive,
                    ActiveState,
                    InactiveState
                },
                cancellationToken: cancellationToken);

            return await connection.ExecuteAsync(command);
        }

        public async Task<int> DeleteEmployeeAsync(long id, CancellationToken cancellationToken = default)
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
