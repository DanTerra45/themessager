using Dapper;
using Mercadito.src.notifications.application.models;
using Mercadito.src.users.domain.entities;
using MySqlConnector;
using Mercadito.src.shared.domain.exceptions;
using System.Data;
using System.Text;
using Mercadito.src.shared.domain.validation;

namespace Mercadito.src.users.infrastructure.persistence
{
    public sealed partial class UserRepository
    {
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
                MustChangePassword = row.MustChangePassword,
                State = row.State,
                LastLogin = row.LastLogin,
                CreatedAt = row.CreatedAt,
                UpdatedAt = row.UpdatedAt
            };
        }

        private static UserRole ParseRole(string value)
        {
            if (Enum.TryParse<UserRole>(value, ignoreCase: false, out var role))
            {
                return role;
            }

            return UserRole.Operador;
        }

        private static string NormalizeUsernameSeed(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "usuario";
            }

            var builder = new StringBuilder(value.Length);
            var previousWasSeparator = false;

            var normalizedValue = ValidationText.NormalizeLowerTrimmed(value);

            foreach (var character in normalizedValue)
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
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return "usuario";
            }

            return normalized;
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
            public bool MustChangePassword { get; set; }
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
