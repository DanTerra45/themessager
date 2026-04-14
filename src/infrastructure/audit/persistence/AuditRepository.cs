using Dapper;
using Mercadito.src.shared.infrastructure.persistence;
using Mercadito.src.application.audit.ports.output;
using Mercadito.src.domain.audit.entities;
using MySqlConnector;
using Mercadito.src.domain.shared.exceptions;

namespace Mercadito.src.infrastructure.audit.persistence
{
    public sealed class AuditRepository(IDbConnectionFactory dbConnectionFactory) : IAuditRepository
    {
        public async Task RegisterAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entry);

            try
            {
                using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

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
            catch (MySqlException exception)
            {
                throw new DataStoreUnavailableException("No se pudo registrar la auditoría porque la base de datos no está disponible.", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw new DataStoreUnavailableException("No se pudo registrar la auditoría porque la base de datos no está disponible.", exception);
            }
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


