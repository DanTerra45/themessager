using Dapper;
using Mercadito.database.interfaces;
using Mercadito.src.employees.data.dto;
using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.repository;
using Microsoft.Extensions.Logging;

namespace Mercadito.src.employees.data.repository
{
    #pragma warning disable S2139 // Permite loggear y relanzar excepciones
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly IDataBaseConnection _dbConnection;
        private readonly ILogger<EmployeeRepository> _logger;
        private const string TableName = "empleados";

        public EmployeeRepository(IDataBaseConnection dbConnection, ILogger<EmployeeRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        #pragma warning disable S2325 // Estos métodos no pueden ser estáticos porque usan campos de instancia
        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $@"SELECT
                    id AS Id,
                    ci AS Ci,
                    COALESCE(complemento, '') AS Complemento,
                    COALESCE(nombres, '') AS Nombres,
                    COALESCE(primerApellido, '') AS PrimerApellido,
                    COALESCE(segundoApellido, '') AS SegundoApellido,
                    COALESCE(rol, '') AS Rol,
                    COALESCE(numeroContacto, '') AS NumeroContacto,
                    (estado = 'A') AS IsActive
                    FROM {TableName}
                    WHERE estado = 'A'";
                return await connection.QueryAsync<Employee>(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los empleados");
                throw;
            }
        }

        public async Task<IEnumerable<Employee>> GetEmployeesByPages(int page, int pageSize = 10)
        {
            try
            {
                int offset = (page - 1) * pageSize;
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $@"SELECT
                    id AS Id,
                    ci AS Ci,
                    COALESCE(complemento, '') AS Complemento,
                    COALESCE(nombres, '') AS Nombres,
                    COALESCE(primerApellido, '') AS PrimerApellido,
                    COALESCE(segundoApellido, '') AS SegundoApellido,
                    COALESCE(rol, '') AS Rol,
                    COALESCE(numeroContacto, '') AS NumeroContacto,
                    (estado = 'A') AS IsActive
                    FROM {TableName}
                    WHERE estado = 'A'
                    ORDER BY primerApellido, segundoApellido, nombres
                    LIMIT @PageSize OFFSET @Offset";
                return await connection.QueryAsync<Employee>(query, new { Offset = offset, PageSize = pageSize });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empleados por página: {Page}", page);
                throw;
            }
        }

        public async Task<int> GetTotalEmployeesCountAsync()
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"SELECT COUNT(*) FROM {TableName} WHERE estado = 'A'";
                return await connection.ExecuteScalarAsync<int>(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al contar empleados");
                throw;
            }
        }

        public async Task<Employee?> GetEmployeeByIdAsync(long id)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $@"SELECT
                    id AS Id,
                    ci AS Ci,
                    COALESCE(complemento, '') AS Complemento,
                    COALESCE(nombres, '') AS Nombres,
                    COALESCE(primerApellido, '') AS PrimerApellido,
                    COALESCE(segundoApellido, '') AS SegundoApellido,
                    COALESCE(rol, '') AS Rol,
                    COALESCE(numeroContacto, '') AS NumeroContacto,
                    (estado = 'A') AS IsActive
                    FROM {TableName}
                    WHERE id = @Id";
                return await connection.QueryFirstOrDefaultAsync<Employee>(query, new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empleado con ID: {Id}", id);
                throw;
            }
        }

        public async Task<long> AddEmployeeAsync(CreateEmployeeDto employee)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $@"INSERT INTO {TableName} 
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar empleado");
                throw;
            }
        }

        public async Task UpdateEmployeeAsync(Employee employee)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $@"UPDATE {TableName} SET
      ci = @Ci,
      complemento = @Complemento,
      nombres = @Nombres,
      primerApellido = @PrimerApellido,
      segundoApellido = @SegundoApellido,
      rol = @Rol,
      numeroContacto = @NumeroContacto,
      estado = CASE WHEN @IsActive THEN 'A' ELSE 'I' END
      WHERE id = @Id";

                await connection.ExecuteAsync(query, new
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar empleado");
                throw;
            }

        }

        public async Task DeleteEmployeeAsync(long id)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"UPDATE {TableName} SET estado = 'I' WHERE id = @Id AND estado = 'A'";
                await connection.ExecuteAsync(query, new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar empleado");
                throw;
            }

        }
#pragma warning restore S2325
    }
    #pragma warning restore S2139
}