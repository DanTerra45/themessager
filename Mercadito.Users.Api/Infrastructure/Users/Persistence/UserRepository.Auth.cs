using Dapper;
using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Domain.Users.Entities;
using MySqlConnector;
using System.Globalization;

namespace Mercadito.Users.Api.Infrastructure.Users.Persistence
{
    public sealed partial class UserRepository
    {
        public async Task<User?> GetActiveByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
                const string query = @"
                        SELECT
                            u.id AS Id,
                            u.username AS Username,
                            u.passwordHash AS PasswordHash,
                            u.Email AS Email,
                            u.empleadoId AS EmployeeId,
                            u.rol AS Role,
                            u.debeCambiarPassword AS MustChangePassword,
                            u.estado AS State,
                            u.ultimoLogin AS LastLogin,
                            u.fechaRegistro AS CreatedAt,
                            u.ultimaActualizacion AS UpdatedAt
                        FROM usuarios u
                        WHERE u.estado = 'A'
                        AND u.username = @Username
                        LIMIT 1;";

                var command = new CommandDefinition(query, new { Username = username }, cancellationToken: cancellationToken);
                var row = await connection.QueryFirstOrDefaultAsync<UserRow>(command);
                if (row == null)
                {
                    return null;
                }

                return Map(row);
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar el usuario activo por nombre de usuario", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar el usuario activo por nombre de usuario", exception);
            }
        }

        public async Task<User?> GetActiveByIdAsync(long userId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
                const string query = @"
                        SELECT
                            u.id AS Id,
                            u.username AS Username,
                            u.passwordHash AS PasswordHash,
                            u.Email AS Email,
                            u.empleadoId AS EmployeeId,
                            u.rol AS Role,
                            u.debeCambiarPassword AS MustChangePassword,
                            u.estado AS State,
                            u.ultimoLogin AS LastLogin,
                            u.fechaRegistro AS CreatedAt,
                            u.ultimaActualizacion AS UpdatedAt
                        FROM usuarios u
                        WHERE u.estado = 'A'
                        AND u.id = @UserId
                        LIMIT 1;";

                var command = new CommandDefinition(query, new { UserId = userId }, cancellationToken: cancellationToken);
                var row = await connection.QueryFirstOrDefaultAsync<UserRow>(command);
                if (row == null)
                {
                    return null;
                }

                return Map(row);
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar el usuario activo por identificador", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar el usuario activo por identificador", exception);
            }
        }

        public async Task<User?> GetActiveByUsernameOrEmailAsync(string identifier, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
                const string query = @"
                        SELECT
                            u.id AS Id,
                            u.username AS Username,
                            u.passwordHash AS PasswordHash,
                            u.Email AS Email,
                            u.empleadoId AS EmployeeId,
                            u.rol AS Role,
                            u.debeCambiarPassword AS MustChangePassword,
                            u.estado AS State,
                            u.ultimoLogin AS LastLogin,
                            u.fechaRegistro AS CreatedAt,
                            u.ultimaActualizacion AS UpdatedAt
                        FROM usuarios u
                        WHERE u.estado = 'A'
                        AND (u.username = @Identifier OR u.Email = @Identifier)
                        LIMIT 1;";

                var command = new CommandDefinition(query, new { Identifier = identifier }, cancellationToken: cancellationToken);
                var row = await connection.QueryFirstOrDefaultAsync<UserRow>(command);
                if (row == null)
                {
                    return null;
                }

                return Map(row);
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar el usuario activo por nombre o correo", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar el usuario activo por nombre o correo", exception);
            }
        }

        public async Task<PasswordResetTokenInfo?> GetValidPasswordResetTokenAsync(string tokenHash, DateTime currentUtc, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
                const string query = @"
                        SELECT
                            t.id AS TokenId,
                            u.id AS UserId,
                            u.username AS Username,
                            u.Email AS Email,
                            t.expiresAtUtc AS ExpiresAtUtc
                        FROM password_reset_tokens t
                        INNER JOIN usuarios u ON u.id = t.usuarioId
                        WHERE t.tokenHash = @TokenHash
                        AND t.usedAtUtc IS NULL
                        AND t.expiresAtUtc > @CurrentUtc
                        AND u.estado = 'A'
                        LIMIT 1;";

                var command = new CommandDefinition(
                    query,
                    new
                    {
                        TokenHash = tokenHash,
                        CurrentUtc = currentUtc
                    },
                    cancellationToken: cancellationToken);

                return await connection.QueryFirstOrDefaultAsync<PasswordResetTokenInfo>(command);
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar el token de restablecimiento", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar el token de restablecimiento", exception);
            }
        }

        public async Task<string> GenerateUniqueUsernameAsync(string seed, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(seed);

            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);

                var localPart = seed;
                var atIndex = seed.IndexOf('@', StringComparison.Ordinal);
                if (atIndex > 0)
                {
                    localPart = seed[..atIndex];
                }

                var baseUsername = NormalizeUsernameSeed(localPart);
                if (baseUsername.Length < 4)
                {
                    baseUsername = "usuario";
                }

                if (baseUsername.Length > 40)
                {
                    baseUsername = baseUsername[..40];
                }

                var candidate = baseUsername;
                var suffix = 1;

                while (await UsernameExistsAsync(connection, candidate, cancellationToken))
                {
                    var suffixText = suffix.ToString(CultureInfo.InvariantCulture);
                    var maxBaseLength = 40 - suffixText.Length;
                    var truncatedBase = baseUsername;

                    if (truncatedBase.Length > maxBaseLength)
                    {
                        truncatedBase = truncatedBase[..maxBaseLength];
                    }

                    candidate = $"{truncatedBase}{suffixText}";
                    suffix++;
                }

                return candidate;
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("generar el nombre de usuario", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("generar el nombre de usuario", exception);
            }
        }

        public async Task UpdateLastLoginAsync(long userId, DateTime lastLogin, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
                const string query = @"
                        UPDATE usuarios
                        SET ultimoLogin = @LastLogin
                        WHERE id = @UserId;";

                var command = new CommandDefinition(query, new { UserId = userId, LastLogin = lastLogin }, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("actualizar el último ingreso", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("actualizar el último ingreso", exception);
            }
        }
    }
}
