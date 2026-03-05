using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Mercadito
{
    #pragma warning disable S2139 // Permite loggear y relanzar excepciones
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly IDataBaseConnection _dbConnection;
        private readonly ILogger<EmployeeRepository> _logger;
        private readonly string tableName = "employees";

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
                var query = $"SELECT id AS Id, firstName AS FirstName, lastName AS LastName, position AS Position, hireDate AS HireDate, salary AS Salary, email AS Email, phone AS Phone, address AS Address, isActive AS IsActive FROM {tableName}";
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
                var query = $"SELECT id AS Id, firstName AS FirstName, lastName AS LastName, position AS Position, hireDate AS HireDate, salary AS Salary, email AS Email, phone AS Phone, address AS Address, isActive AS IsActive FROM {tableName} ORDER BY lastName, firstName LIMIT @PageSize OFFSET @Offset";
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
                var query = $"SELECT id AS Id, firstName AS FirstName, lastName AS LastName, position AS Position, hireDate AS HireDate, salary AS Salary, email AS Email, phone AS Phone, address AS Address, isActive AS IsActive FROM {tableName} WHERE id = @Id";
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
                    (id, firstName, lastName, position, hireDate, salary, email, phone, address, isActive) 
                    VALUES 
                    (@Id, @FirstName, @LastName, @Position, @HireDate, @Salary, @Email, @Phone, @Address, @IsActive)";
                
                var result = await connection.ExecuteAsync(query, new
                {
                    Id = employeeId,
                    employee.FirstName,
                    employee.LastName,
                    employee.Position,
                    employee.HireDate,
                    employee.Salary,
                    employee.Email,
                    employee.Phone,
                    employee.Address,
                    IsActive = true
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
                    firstName = @FirstName,
                    lastName = @LastName,
                    position = @Position,
                    hireDate = @HireDate,
                    salary = @Salary,
                    email = @Email,
                    phone = @Phone,
                    address = @Address,
                    isActive = @IsActive
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
        #pragma warning restore S2325
    }
    #pragma warning restore S2139
}