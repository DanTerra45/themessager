using Dapper;
using Mercadito.database.interfaces;
using Mercadito.src.notifications.application.models;
using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.users.domain.entities;
using MySqlConnector;
using Shared.Domain;
using System.Data;
using System.Text;

namespace Mercadito.src.users.infrastructure.persistence
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public UserRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<User?> GetActiveByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            const string query = @"
SELECT
    u.id AS Id,
    u.username AS Username,
    u.passwordHash AS PasswordHash,
    u.email AS Email,
    u.empleadoId AS EmployeeId,
    u.rol AS Role,
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
            return row == null ? null : Map(row);
        }

        public async Task<User?> GetActiveByIdAsync(long userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            const string query = @"
SELECT
    u.id AS Id,
    u.username AS Username,
    u.passwordHash AS PasswordHash,
    u.email AS Email,
    u.empleadoId AS EmployeeId,
    u.rol AS Role,
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
            return row == null ? null : Map(row);
        }

        public async Task<User?> GetActiveByUsernameOrEmailAsync(string identifier, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            const string query = @"
SELECT
    u.id AS Id,
    u.username AS Username,
    u.passwordHash AS PasswordHash,
    u.email AS Email,
    u.empleadoId AS EmployeeId,
    u.rol AS Role,
    u.estado AS State,
    u.ultimoLogin AS LastLogin,
    u.fechaRegistro AS CreatedAt,
    u.ultimaActualizacion AS UpdatedAt
FROM usuarios u
WHERE u.estado = 'A'
  AND (u.username = @Identifier OR u.email = @Identifier)
LIMIT 1;";

            var command = new CommandDefinition(query, new { Identifier = identifier }, cancellationToken: cancellationToken);
            var row = await connection.QueryFirstOrDefaultAsync<UserRow>(command);
            return row == null ? null : Map(row);
        }

        public async Task<PasswordResetTokenInfo?> GetValidPasswordResetTokenAsync(string tokenHash, DateTime currentUtc, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            const string query = @"
SELECT
    t.id AS TokenId,
    u.id AS UserId,
    u.username AS Username,
    u.email AS Email,
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

        public async Task<string> GenerateUniqueUsernameAsync(string seed, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);

            var localPart = seed;
            var atIndex = seed.IndexOf('@');
            if (atIndex > 0)
            {
                localPart = seed.Substring(0, atIndex);
            }

            var baseUsername = NormalizeUsernameSeed(localPart);
            if (baseUsername.Length < 4)
            {
                baseUsername = "usuario";
            }

            if (baseUsername.Length > 40)
            {
                baseUsername = baseUsername.Substring(0, 40);
            }

            var candidate = baseUsername;
            var suffix = 1;

            while (await UsernameExistsAsync(connection, candidate, cancellationToken))
            {
                var suffixText = suffix.ToString();
                var maxBaseLength = 40 - suffixText.Length;
                var truncatedBase = baseUsername;

                if (truncatedBase.Length > maxBaseLength)
                {
                    truncatedBase = truncatedBase.Substring(0, maxBaseLength);
                }

                candidate = $"{truncatedBase}{suffixText}";
                suffix++;
            }

            return candidate;
        }

        public async Task UpdateLastLoginAsync(long userId, DateTime lastLogin, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            const string query = @"
UPDATE usuarios
SET ultimoLogin = @LastLogin
WHERE id = @UserId;";

            var command = new CommandDefinition(query, new { UserId = userId, LastLogin = lastLogin }, cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);
        }

        public async Task<long> CreateAsync(CreateUserDto user, string passwordHash, CancellationToken cancellationToken = default)
        {
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
INSERT INTO usuarios (username, passwordHash, email, empleadoId, rol, estado)
VALUES (@Username, @PasswordHash, @Email, @EmployeeId, @Role, 'A');
SELECT LAST_INSERT_ID();";

                var parameters = new
                {
                    user.Username,
                    PasswordHash = passwordHash,
                    Email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email,
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
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<long> CreateWithOnboardingAsync(CreateUserDto user, string passwordHash, string tokenHash, DateTime expiresAtUtc, EmailMessage onboardingEmail, CancellationToken cancellationToken = default)
        {
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
INSERT INTO usuarios (username, passwordHash, email, empleadoId, rol, estado)
VALUES (@Username, @PasswordHash, @Email, @EmployeeId, @Role, 'A');
SELECT LAST_INSERT_ID();";

                var insertUserCommand = new CommandDefinition(
                    insertUserQuery,
                    new
                    {
                        user.Username,
                        PasswordHash = passwordHash,
                        Email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email,
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
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(long userId, string passwordHash, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            const string query = @"
UPDATE usuarios
SET passwordHash = @PasswordHash
WHERE id = @UserId
  AND estado = 'A';";

            var command = new CommandDefinition(
                query,
                new
                {
                    UserId = userId,
                    PasswordHash = passwordHash
                },
                cancellationToken: cancellationToken);

            var affectedRows = await connection.ExecuteAsync(command);
            return affectedRows > 0;
        }

        public async Task<bool> ResetPasswordAndQueueNotificationAsync(long userId, string passwordHash, EmailMessage? notificationEmail, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                const string updateQuery = @"
UPDATE usuarios
SET passwordHash = @PasswordHash
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

                if (notificationEmail != null)
                {
                    await QueueEmailAsync(connection, transaction, notificationEmail, cancellationToken);
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> DeactivateAsync(long userId, CancellationToken cancellationToken = default)
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

        public async Task CreatePasswordResetTokenAsync(long userId, string tokenHash, DateTime expiresAtUtc, DateTime invalidatedAtUtc, CancellationToken cancellationToken = default)
        {
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

                const string insertQuery = @"
INSERT INTO password_reset_tokens (usuarioId, tokenHash, expiresAtUtc)
VALUES (@UserId, @TokenHash, @ExpiresAtUtc);";

                var insertCommand = new CommandDefinition(
                    insertQuery,
                    new
                    {
                        UserId = userId,
                        TokenHash = tokenHash,
                        ExpiresAtUtc = expiresAtUtc
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(insertCommand);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task CreatePasswordResetTokenAndQueueEmailAsync(long userId, string tokenHash, DateTime expiresAtUtc, DateTime invalidatedAtUtc, EmailMessage emailMessage, CancellationToken cancellationToken = default)
        {
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
            catch
            {
                transaction.Rollback();
                throw;
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
SET passwordHash = @PasswordHash
WHERE id = @UserId
  AND estado = 'A';";

                var updateUserCommand = new CommandDefinition(
                    updateUserQuery,
                    new
                    {
                        PasswordHash = passwordHash,
                        UserId = tokenRow.UserId
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
                        UserId = tokenRow.UserId,
                        UsedAtUtc = currentUtc
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(consumeCommand);
                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IReadOnlyList<UserListItem>> GetAllActiveAsync(CancellationToken cancellationToken = default)
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

            return rows.Select(row => new UserListItem
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
            }).ToList();
        }

        public async Task<IReadOnlyList<AvailableEmployeeOption>> GetAvailableEmployeesAsync(CancellationToken cancellationToken = default)
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
            return rows.ToList();
        }

        private static User Map(UserRow row)
        {
            return new User
            {
                Id = row.Id,
                Username = row.Username,
                PasswordHash = row.PasswordHash,
                Email = row.Email,
                EmployeeId = row.EmployeeId,
                Role = ParseRole(row.Role),
                State = row.State,
                LastLogin = row.LastLogin,
                CreatedAt = row.CreatedAt,
                UpdatedAt = row.UpdatedAt
            };
        }

        private static UserRole ParseRole(string value)
        {
            return Enum.TryParse<UserRole>(value, ignoreCase: false, out var role) ? role : UserRole.Operador;
        }

        private static string NormalizeUsernameSeed(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "usuario";
            }

            var builder = new StringBuilder(value.Length);
            var previousWasSeparator = false;

            foreach (var character in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                    previousWasSeparator = false;
                    continue;
                }

                if (character == '.' || character == '_' || character == '-')
                {
                    if (previousWasSeparator)
                    {
                        continue;
                    }

                    builder.Append(character);
                    previousWasSeparator = true;
                }
            }

            var normalized = builder.ToString().Trim('.', '_', '-');
            return string.IsNullOrWhiteSpace(normalized) ? "usuario" : normalized;
        }

        private static async Task<bool> UsernameExistsAsync(IDbConnection connection, string username, CancellationToken cancellationToken)
        {
            const string query = @"
SELECT EXISTS (
    SELECT 1
    FROM usuarios
    WHERE username = @Username
) AS UsernameExists;";

            var command = new CommandDefinition(query, new { Username = username }, cancellationToken: cancellationToken);
            return await connection.ExecuteScalarAsync<bool>(command);
        }

        private static async Task QueueEmailAsync(IDbConnection connection, IDbTransaction transaction, EmailMessage emailMessage, CancellationToken cancellationToken)
        {
            const string query = @"
INSERT INTO email_outbox (toAddress, toName, subject, plainTextBody, htmlBody, status, nextAttemptAtUtc)
VALUES (@ToAddress, @ToName, @Subject, @PlainTextBody, @HtmlBody, 'P', UTC_TIMESTAMP());";

            var command = new CommandDefinition(
                query,
                new
                {
                    emailMessage.ToAddress,
                    emailMessage.ToName,
                    emailMessage.Subject,
                    emailMessage.PlainTextBody,
                    emailMessage.HtmlBody
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }

        private static BusinessValidationException MapDuplicateException(MySqlException exception)
        {
            var message = exception.Message;

            if (message.Contains("uq_usuarios_username", StringComparison.OrdinalIgnoreCase))
            {
                return new BusinessValidationException("Username", "Ya existe un usuario con ese nombre.");
            }

            if (message.Contains("uq_usuarios_email", StringComparison.OrdinalIgnoreCase))
            {
                return new BusinessValidationException("Email", "Ya existe un usuario con ese correo.");
            }

            if (message.Contains("uq_usuarios_activos_empleado", StringComparison.OrdinalIgnoreCase))
            {
                return new BusinessValidationException("EmployeeId", "El empleado seleccionado ya tiene un usuario activo.");
            }

            return new BusinessValidationException("No se pudo registrar el usuario por una restricción única.");
        }

        private sealed class UserRow
        {
            public long Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string PasswordHash { get; set; } = string.Empty;
            public string? Email { get; set; }
            public long? EmployeeId { get; set; }
            public string Role { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public DateTime? LastLogin { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        private sealed class UserListItemRow
        {
            public long Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string Role { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public DateTime? LastLogin { get; set; }
            public DateTime CreatedAt { get; set; }
            public long? EmployeeId { get; set; }
            public string? EmployeeFullName { get; set; }
            public string? EmployeeCargo { get; set; }
        }

        private sealed class PasswordResetTokenRow
        {
            public long TokenId { get; set; }
            public long UserId { get; set; }
        }
    }
}
