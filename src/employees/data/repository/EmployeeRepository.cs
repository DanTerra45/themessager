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
                    numeroContacto AS NumeroContacto 
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
                    numeroContacto AS NumeroContacto 
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

        public async Task<Employee?> GetEmployeeByIdAsync(Guid id)
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
                    numeroContacto AS NumeroContacto 
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

        public async Task<Guid> AddEmployeeAsync(CreateEmployeeDto employee)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var employeeId = Guid.NewGuid();
                var query = $@"INSERT INTO {tableName} 
                    (id, ci, complemento, nombres, primerApellido, segundoApellido, rol, numeroContacto) 
                    VALUES 
                    (@Id, @Ci, @Complemento, @Nombres, @PrimerApellido, @SegundoApellido, @Rol, @NumeroContacto)";
                
                var result = await connection.ExecuteAsync(query, new
                {
                    Id = employeeId,
                    employee.Ci,
                    Complemento = employee.Complemento ?? "",
                    employee.Nombres,
                    employee.PrimerApellido,
                    SegundoApellido = employee.SegundoApellido ?? "",
                    employee.Rol,
                    employee.NumeroContacto
                });
                return result > 0 ? employeeId : Guid.Empty;
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
                var query = $@"UPDATE {tableName} SET 
                    ci = @Ci,
                    complemento = @Complemento,
                    nombres = @Nombres,
                    primerApellido = @PrimerApellido,
                    segundoApellido = @SegundoApellido,
                    rol = @Rol,
                    numeroContacto = @NumeroContacto
                    WHERE id = @Id";
                
                await connection.ExecuteAsync(query, employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar empleado");
                throw;
            }
        }

        public async Task DeleteEmployeeAsync(Guid id)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"DELETE FROM {tableName} WHERE id = @Id";
                await connection.ExecuteAsync(query, new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar empleado");
                throw;
            }
        }
    }
    #pragma warning restore S2139
}