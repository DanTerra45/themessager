using Dapper;
using Mercadito.database.interfaces;
using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.repository;

namespace Mercadito.src.employees.data.repository
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly IDataBaseConnection _dbConnection;

        public EmployeeRepository(IDataBaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<Employee>> GetEmployeesByPages(int page, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            int offset = (page - 1) * pageSize;
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
                    (estado = 'A') AS IsActive
                    FROM empleados
                    WHERE estado = 'A'
                    ORDER BY primerApellido, segundoApellido, nombres
                    LIMIT @PageSize OFFSET @Offset";
            return await connection.QueryAsync<Employee>(query, new { Offset = offset, PageSize = pageSize });
        }

        public async Task<int> GetTotalEmployeesCountAsync(CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = "SELECT COUNT(*) FROM empleados WHERE estado = 'A'";
            return await connection.ExecuteScalarAsync<int>(query);
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
                    (estado = 'A') AS IsActive
                    FROM empleados
                    WHERE id = @Id AND estado = 'A'";
            return await connection.QueryFirstOrDefaultAsync<Employee>(query, new { Id = id });
        }

        public async Task<long> AddEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"INSERT INTO empleados 
                    (ci, complemento, nombres, primerApellido, segundoApellido, rol, numeroContacto, estado) 
                    VALUES 
                    (@Ci, @Complemento, @Nombres, @PrimerApellido, @SegundoApellido, @Rol, @NumeroContacto, 'A')";

            await connection.ExecuteAsync(query, new
            {
                employee.Ci,
                employee.Complemento,
                employee.Nombres,
                employee.PrimerApellido,
                employee.SegundoApellido,
                employee.Rol,
                employee.NumeroContacto
            });

            return await connection.ExecuteScalarAsync<long>("SELECT LAST_INSERT_ID();");
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
      estado = CASE WHEN @IsActive THEN 'A' ELSE 'I' END
    WHERE id = @Id AND estado = 'A'";

            return await connection.ExecuteAsync(query, new
            {
                employee.Id,
                employee.Ci,
                employee.Complemento,
                employee.Nombres,
                employee.PrimerApellido,
                employee.SegundoApellido,
                employee.Rol,
                employee.NumeroContacto,
                employee.IsActive
            });
        }

        public async Task<int> DeleteEmployeeAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = "UPDATE empleados SET estado = 'I' WHERE id = @Id AND estado = 'A'";
            return await connection.ExecuteAsync(query, new { Id = id });
        }
    }
}
