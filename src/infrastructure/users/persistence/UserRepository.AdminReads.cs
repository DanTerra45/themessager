using Dapper;
using Mercadito.src.application.users.models;
using MySqlConnector;
using System.Data;

namespace Mercadito.src.infrastructure.users.persistence
{
    public sealed partial class UserRepository
    {
        public async Task<IReadOnlyList<UserListItem>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
                const string query = @"
                        SELECT
                            u.id AS Id,
                            u.username AS Username,
                            u.email AS Email,
                            u.rol AS Role,
                            u.estado AS State,
                            u.ultimoLogin AS LastLogin,
                            u.fechaRegistro AS CreatedAt,
                            u.empleadoId AS EmployeeId,
                            CASE
                                WHEN e.id IS NULL THEN NULL
                                WHEN e.segundoApellido IS NULL OR e.segundoApellido = '' THEN CONCAT(e.primerApellido, ', ', e.nombres)
                                ELSE CONCAT(e.primerApellido, ' ', e.segundoApellido, ', ', e.nombres)
                            END AS EmployeeFullName,
                            e.cargo AS EmployeeCargo
                        FROM usuarios u
                        LEFT JOIN empleados e ON e.id = u.empleadoId
                        WHERE u.estado = 'A'
                        ORDER BY u.username ASC, u.id ASC;";

                var command = new CommandDefinition(query, cancellationToken: cancellationToken);
                var rows = await connection.QueryAsync<UserListItemRow>(command);

                return [.. rows.Select(row => new UserListItem
                {
                    Id = row.Id,
                    Username = row.Username,
                    Email = row.Email,
                    Role = ParseRole(row.Role),
                    State = row.State,
                    LastLogin = row.LastLogin,
                    CreatedAt = row.CreatedAt,
                    EmployeeId = row.EmployeeId,
                    EmployeeFullName = row.EmployeeFullName,
                    EmployeeCargo = row.EmployeeCargo
                })];
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar los usuarios activos", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar los usuarios activos", exception);
            }
        }

        public async Task<IReadOnlyList<AvailableEmployeeOption>> GetAvailableEmployeesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
                const string query = @"
                        SELECT
                            e.id AS Id,
                            CASE
                                WHEN e.segundoApellido IS NULL OR e.segundoApellido = '' THEN CONCAT(e.primerApellido, ', ', e.nombres)
                                ELSE CONCAT(e.primerApellido, ' ', e.segundoApellido, ', ', e.nombres)
                            END AS FullName,
                            e.cargo AS Cargo,
                            CASE
                                WHEN e.complemento IS NULL OR e.complemento = '' THEN CAST(e.ci AS CHAR)
                                ELSE CONCAT(CAST(e.ci AS CHAR), '-', e.complemento)
                            END AS CiDisplay
                        FROM empleados e
                        LEFT JOIN usuarios u
                            ON u.empleadoId = e.id
                        AND u.estado = 'A'
                        WHERE e.estado = 'A'
                        AND u.id IS NULL
                        ORDER BY e.primerApellido ASC, e.segundoApellido ASC, e.nombres ASC, e.id ASC;";

                var command = new CommandDefinition(query, cancellationToken: cancellationToken);
                var rows = await connection.QueryAsync<AvailableEmployeeOption>(command);
                return [.. rows];
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar los empleados disponibles", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar los empleados disponibles", exception);
            }
        }
    }
}
