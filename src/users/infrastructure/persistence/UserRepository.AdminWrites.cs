using Dapper;
using Mercadito.src.notifications.application.models;
using Mercadito.src.users.application.models;
using MySqlConnector;
using Mercadito.src.shared.domain.exceptions;

namespace Mercadito.src.users.infrastructure.persistence
{
    public sealed partial class UserRepository
    {
        public async Task<long> CreateAsync(CreateUserDto user, string passwordHash, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(user);

            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                if (user.EmployeeId.HasValue)
                {
                    const string employeeQuery = @"
                            SELECT EXISTS (
                                SELECT 1
                                FROM empleados e
                                LEFT JOIN usuarios u
                                    ON u.empleadoId = e.id
                                AND u.estado = 'A'
                                WHERE e.id = @EmployeeId
                                AND e.estado = 'A'
                                AND u.id IS NULL
                            ) AS IsAvailable;";

                    var employeeCommand = new CommandDefinition(
                        employeeQuery,
                        new { EmployeeId = user.EmployeeId.Value },
                        transaction: transaction,
                        cancellationToken: cancellationToken);

                    var employeeAvailable = await connection.ExecuteScalarAsync<bool>(employeeCommand);
                    if (!employeeAvailable)
                    {
                        transaction.Rollback();
                        throw new BusinessValidationException("EmployeeId", "El empleado seleccionado no está disponible para asociar un usuario.");
                    }
                }

                const string query = @"
                        INSERT INTO usuarios (username, passwordHash, email, empleadoId, rol, debeCambiarPassword, estado)
                        VALUES (@Username, @PasswordHash, @Email, @EmployeeId, @Role, 0, 'A');
                        SELECT LAST_INSERT_ID();";

                string? email = null;
                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    email = user.Email;
                }

                var parameters = new
                {
                    user.Username,
                    PasswordHash = passwordHash,
                    Email = email,
                    user.EmployeeId,
                    user.Role
                };

                var command = new CommandDefinition(query, parameters, transaction: transaction, cancellationToken: cancellationToken);
                var userId = await connection.ExecuteScalarAsync<long>(command);
                transaction.Commit();
                return userId;
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                transaction.Rollback();
                throw MapDuplicateException(exception);
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("crear el usuario", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("crear el usuario", exception);
            }
        }

        public async Task<long> CreateWithOnboardingAsync(CreateUserDto user, string passwordHash, string tokenHash, DateTime expiresAtUtc, EmailMessage onboardingEmail, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(onboardingEmail);

            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                if (user.EmployeeId.HasValue)
                {
                    const string employeeQuery = @"
                            SELECT EXISTS (
                                SELECT 1
                                FROM empleados e
                                LEFT JOIN usuarios u
                                    ON u.empleadoId = e.id
                                AND u.estado = 'A'
                                WHERE e.id = @EmployeeId
                                AND e.estado = 'A'
                                AND u.id IS NULL
                            ) AS IsAvailable;";

                    var employeeCommand = new CommandDefinition(
                        employeeQuery,
                        new { EmployeeId = user.EmployeeId.Value },
                        transaction: transaction,
                        cancellationToken: cancellationToken);

                    var employeeAvailable = await connection.ExecuteScalarAsync<bool>(employeeCommand);
                    if (!employeeAvailable)
                    {
                        transaction.Rollback();
                        throw new BusinessValidationException("EmployeeId", "El empleado seleccionado no está disponible para asociar un usuario.");
                    }
                }

                const string insertUserQuery = @"
                        INSERT INTO usuarios (username, passwordHash, email, empleadoId, rol, debeCambiarPassword, estado)
                        VALUES (@Username, @PasswordHash, @Email, @EmployeeId, @Role, 0, 'A');
                        SELECT LAST_INSERT_ID();";

                string? email = null;
                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    email = user.Email;
                }

                var insertUserCommand = new CommandDefinition(
                    insertUserQuery,
                    new
                    {
                        user.Username,
                        PasswordHash = passwordHash,
                        Email = email,
                        user.EmployeeId,
                        user.Role
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var userId = await connection.ExecuteScalarAsync<long>(insertUserCommand);

                const string insertTokenQuery = @"
                        INSERT INTO password_reset_tokens (usuarioId, tokenHash, expiresAtUtc)
                        VALUES (@UserId, @TokenHash, @ExpiresAtUtc);";

                var insertTokenCommand = new CommandDefinition(
                    insertTokenQuery,
                    new
                    {
                        UserId = userId,
                        TokenHash = tokenHash,
                        ExpiresAtUtc = expiresAtUtc
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(insertTokenCommand);
                await QueueEmailAsync(connection, transaction, onboardingEmail, cancellationToken);
                transaction.Commit();
                return userId;
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                transaction.Rollback();
                throw MapDuplicateException(exception);
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("crear el usuario con onboarding", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("crear el usuario con onboarding", exception);
            }
        }

        public async Task<bool> BeginAdministrativePasswordResetAsync(long userId, string passwordHash, string tokenHash, DateTime expiresAtUtc, DateTime invalidatedAtUtc, EmailMessage emailMessage, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                const string updateQuery = @"
                        UPDATE usuarios
                        SET passwordHash = @PasswordHash,
                            debeCambiarPassword = 0
                        WHERE id = @UserId
                        AND estado = 'A';";

                var updateCommand = new CommandDefinition(
                    updateQuery,
                    new
                    {
                        UserId = userId,
                        PasswordHash = passwordHash
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var affectedRows = await connection.ExecuteAsync(updateCommand);
                if (affectedRows == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                const string invalidateTokensQuery = @"
                        UPDATE password_reset_tokens
                        SET usedAtUtc = @InvalidatedAtUtc
                        WHERE usuarioId = @UserId
                        AND usedAtUtc IS NULL;";

                var invalidateTokensCommand = new CommandDefinition(
                    invalidateTokensQuery,
                    new
                    {
                        UserId = userId,
                        InvalidatedAtUtc = invalidatedAtUtc
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(invalidateTokensCommand);

                const string insertTokenQuery = @"
                        INSERT INTO password_reset_tokens (usuarioId, tokenHash, expiresAtUtc)
                        VALUES (@UserId, @TokenHash, @ExpiresAtUtc);";

                var insertTokenCommand = new CommandDefinition(
                    insertTokenQuery,
                    new
                    {
                        UserId = userId,
                        TokenHash = tokenHash,
                        ExpiresAtUtc = expiresAtUtc
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(insertTokenCommand);
                await QueueEmailAsync(connection, transaction, emailMessage, cancellationToken);

                transaction.Commit();
                return true;
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("iniciar el restablecimiento administrativo de contraseña", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("iniciar el restablecimiento administrativo de contraseña", exception);
            }
        }

        public async Task<bool> SetTemporaryPasswordAsync(long userId, string passwordHash, DateTime invalidatedAtUtc, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                const string updateQuery = @"
                        UPDATE usuarios
                        SET passwordHash = @PasswordHash,
                            debeCambiarPassword = 1
                        WHERE id = @UserId
                        AND estado = 'A';";

                var updateCommand = new CommandDefinition(
                    updateQuery,
                    new
                    {
                        UserId = userId,
                        PasswordHash = passwordHash
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var affectedRows = await connection.ExecuteAsync(updateCommand);
                if (affectedRows == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                const string invalidateTokensQuery = @"
                        UPDATE password_reset_tokens
                        SET usedAtUtc = @InvalidatedAtUtc
                        WHERE usuarioId = @UserId
                        AND usedAtUtc IS NULL;";

                var invalidateTokensCommand = new CommandDefinition(
                    invalidateTokensQuery,
                    new
                    {
                        UserId = userId,
                        InvalidatedAtUtc = invalidatedAtUtc
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(invalidateTokensCommand);
                transaction.Commit();
                return true;
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("asignar la contraseña temporal", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("asignar la contraseña temporal", exception);
            }
        }

        public async Task<bool> DeactivateAsync(long userId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
                const string query = @"
                        UPDATE usuarios
                        SET estado = 'I'
                        WHERE id = @UserId
                        AND estado = 'A';";

                var command = new CommandDefinition(
                    query,
                    new { UserId = userId },
                    cancellationToken: cancellationToken);

                var affectedRows = await connection.ExecuteAsync(command);
                return affectedRows > 0;
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("desactivar el usuario", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("desactivar el usuario", exception);
            }
        }
    }
}
