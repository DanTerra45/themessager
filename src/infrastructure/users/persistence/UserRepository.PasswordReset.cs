using Dapper;
using Mercadito.src.application.notifications.models;
using MySqlConnector;

namespace Mercadito.src.infrastructure.users.persistence
{
    public sealed partial class UserRepository
    {
        public async Task CreatePasswordResetTokenAndQueueEmailAsync(long userId, string tokenHash, DateTime expiresAtUtc, DateTime invalidatedAtUtc, EmailMessage emailMessage, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(emailMessage);

            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                const string invalidateQuery = @"
                        UPDATE password_reset_tokens
                        SET usedAtUtc = @InvalidatedAtUtc
                        WHERE usuarioId = @UserId
                        AND usedAtUtc IS NULL;";

                var invalidateCommand = new CommandDefinition(
                    invalidateQuery,
                    new
                    {
                        UserId = userId,
                        InvalidatedAtUtc = invalidatedAtUtc
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(invalidateCommand);

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
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("crear el token de restablecimiento y encolar el correo", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("crear el token de restablecimiento y encolar el correo", exception);
            }
        }

        public async Task<bool> ResetPasswordByTokenAsync(string tokenHash, string passwordHash, DateTime currentUtc, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                const string tokenQuery = @"
                        SELECT
                            t.id AS TokenId,
                            t.usuarioId AS UserId
                        FROM password_reset_tokens t
                        INNER JOIN usuarios u ON u.id = t.usuarioId
                        WHERE t.tokenHash = @TokenHash
                        AND t.usedAtUtc IS NULL
                        AND t.expiresAtUtc > @CurrentUtc
                        AND u.estado = 'A'
                        LIMIT 1
                        FOR UPDATE;";

                var tokenCommand = new CommandDefinition(
                    tokenQuery,
                    new
                    {
                        TokenHash = tokenHash,
                        CurrentUtc = currentUtc
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var tokenRow = await connection.QueryFirstOrDefaultAsync<PasswordResetTokenRow>(tokenCommand);
                if (tokenRow == null)
                {
                    transaction.Rollback();
                    return false;
                }

                const string updateUserQuery = @"
                        UPDATE usuarios
                        SET passwordHash = @PasswordHash,
                            debeCambiarPassword = 0
                        WHERE id = @UserId
                        AND estado = 'A';";

                var updateUserCommand = new CommandDefinition(
                    updateUserQuery,
                    new
                    {
                        PasswordHash = passwordHash,
                        tokenRow.UserId
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var affectedUsers = await connection.ExecuteAsync(updateUserCommand);
                if (affectedUsers == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                const string consumeQuery = @"
                        UPDATE password_reset_tokens
                        SET usedAtUtc = @UsedAtUtc
                        WHERE usuarioId = @UserId
                        AND usedAtUtc IS NULL;";

                var consumeCommand = new CommandDefinition(
                    consumeQuery,
                    new
                    {
                        tokenRow.UserId,
                        UsedAtUtc = currentUtc
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(consumeCommand);
                transaction.Commit();
                return true;
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("restablecer la contraseña por token", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("restablecer la contraseña por token", exception);
            }
        }

        public async Task<bool> CompleteForcedPasswordChangeAsync(long userId, string passwordHash, DateTime invalidatedAtUtc, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                const string updateUserQuery = @"
                        UPDATE usuarios
                        SET passwordHash = @PasswordHash,
                            debeCambiarPassword = 0
                        WHERE id = @UserId
                        AND estado = 'A';";

                var updateUserCommand = new CommandDefinition(
                    updateUserQuery,
                    new
                    {
                        UserId = userId,
                        PasswordHash = passwordHash
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var affectedUsers = await connection.ExecuteAsync(updateUserCommand);
                if (affectedUsers == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                const string consumeQuery = @"
                        UPDATE password_reset_tokens
                        SET usedAtUtc = @UsedAtUtc
                        WHERE usuarioId = @UserId
                        AND usedAtUtc IS NULL;";

                var consumeCommand = new CommandDefinition(
                    consumeQuery,
                    new
                    {
                        UserId = userId,
                        UsedAtUtc = invalidatedAtUtc
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(consumeCommand);
                transaction.Commit();
                return true;
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("completar el cambio obligatorio de contraseña", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("completar el cambio obligatorio de contraseña", exception);
            }
        }

    }
}
