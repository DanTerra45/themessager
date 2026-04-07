using Dapper;
using Mercadito.database.interfaces;
using Mercadito.src.audit.application.ports.output;
using Mercadito.src.audit.domain.entities;

namespace Mercadito.src.audit.infrastructure.persistence
{
    public sealed class AuditRepository : IAuditRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public AuditRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task RegisterAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);

            const string query = @"
INSERT INTO auditoria
    (usuarioId, usuarioUsername, accion, tabla, registroId, ipAddress, userAgent, datosAnteriores, datosNuevos, timestamp)
VALUES
    (@UserId, @Username, @Action, @TableName, @RecordId, @IpAddress, @UserAgent, @PreviousDataJson, @NewDataJson, @Timestamp);";

            var command = new CommandDefinition(
                query,
                new
                {
                    entry.UserId,
                    entry.Username,
                    Action = ToDatabaseAction(entry.Action),
                    entry.TableName,
                    entry.RecordId,
                    entry.IpAddress,
                    entry.UserAgent,
                    entry.PreviousDataJson,
                    entry.NewDataJson,
                    entry.Timestamp
                },
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }

        private static string ToDatabaseAction(AuditAction action)
        {
            return action switch
            {
                AuditAction.Create => "C",
                AuditAction.Update => "U",
                AuditAction.Delete => "D",
                _ => "U"
            };
        }
    }
}
