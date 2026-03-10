using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Mercadito.database.interfaces;

namespace Mercadito
{
    #pragma warning disable S2139
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly IDataBaseConnection _dbConnection;
        private readonly ILogger<EmployeeRepository> _logger;
        private readonly string tableName = "empleados";

        public EmployeeRepository(IDataBaseConnection dbConnection, ILogger<EmployeeRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $@"SELECT 
                    id AS Id, 
                    ci AS Ci, 
                    complemento AS Complemento, 
                    nombres AS Nombres, 
                    primerApellido AS PrimerApellido, 
                    segundoApellido AS SegundoApellido, 
                    rol AS Rol, 
                    numeroContacto AS NumeroContacto,
                    estado AS Estado
                FROM {tableName}";
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
                    complemento AS Complemento, 
                    nombres AS Nombres, 
                    primerApellido AS PrimerApellido, 
                    segundoApellido AS SegundoApellido, 
                    rol AS Rol, 
                    numeroContacto AS NumeroContacto,
                    estado AS Estado
                FROM {tableName} 
                ORDER BY primerApellido, nombres 
                LIMIT @PageSize OFFSET @Offset";
                return await connection.QueryAsync<Employee>(query, new { Offset = offset, PageSize = pageSize });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empleados por página: {Page}", page);
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
                    complemento AS Complemento, 
                    nombres AS Nombres, 
                    primerApellido AS PrimerApellido, 
                    segundoApellido AS SegundoApellido, 
                    rol AS Rol, 
                    numeroContacto AS NumeroContacto,
                    estado AS Estado
                FROM {tableName} 
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
        _logger.LogWarning("Insertando empleado en BD...");

        var insertQuery = $@"INSERT INTO {tableName} 
            (ci, complemento, nombres, primerApellido, segundoApellido, rol, numeroContacto, estado) 
            VALUES 
            (@Ci, @Complemento, @Nombres, @PrimerApellido, @SegundoApellido, @Rol, @NumeroContacto, 'A')";

        var parameters = new
        {
            employee.Ci,
            Complemento = string.IsNullOrEmpty(employee.Complemento) ? null : employee.Complemento,
            employee.Nombres,
            employee.PrimerApellido,
            SegundoApellido = string.IsNullOrEmpty(employee.SegundoApellido) ? null : employee.SegundoApellido,
            employee.Rol,
            employee.NumeroContacto
        };

        var affected = await connection.ExecuteAsync(insertQuery, parameters);
        _logger.LogWarning("Filas insertadas: {Affected}", affected);

        var id = await connection.QuerySingleAsync<long>("SELECT LAST_INSERT_ID();");
        _logger.LogWarning("ID generado: {Id}", id);
        return id;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error en repositorio al agregar empleado. Datos: {@Employee}", employee);
        throw;
    }
}

        public async Task UpdateEmployeeAsync(Employee employee)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $@"UPDATE {tableName} SET 
                    ci = @Ci,
                    complemento = @Complemento,
                    nombres = @Nombres,
                    primerApellido = @PrimerApellido,
                    segundoApellido = @SegundoApellido,
                    rol = @Rol,
                    numeroContacto = @NumeroContacto,
                    estado = @Estado
                    WHERE id = @Id";

                await connection.ExecuteAsync(query, employee);
                _logger.LogInformation("Empleado actualizado con ID: {Id}", employee.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar empleado. Datos: {@Employee}", employee);
                throw;
            }
        }

        public async Task DeleteEmployeeAsync(long id)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"DELETE FROM {tableName} WHERE id = @Id";
                await connection.ExecuteAsync(query, new { Id = id });
                _logger.LogInformation("Empleado eliminado con ID: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar empleado con ID: {Id}", id);
                throw;
            }
        }
    }
    #pragma warning restore S2139
}