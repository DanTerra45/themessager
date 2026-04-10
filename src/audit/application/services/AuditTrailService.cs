using System.Text.Json;
using Mercadito.src.audit.application.ports.input;
using Mercadito.src.audit.domain.entities;
using Mercadito.src.shared.domain;

namespace Mercadito.src.audit.application.services
{
    public sealed class AuditTrailService(
        IRegisterAuditEntryUseCase registerAuditEntryUseCase,
        ILogger<AuditTrailService> logger) : IAuditTrailService
    {
        public Result ValidateActor(AuditActor actor)
        {
            ArgumentNullException.ThrowIfNull(actor);

            if (actor == null || actor.UserId <= 0 || string.IsNullOrWhiteSpace(actor.Username))
            {
                return Result.Failure("Se requiere un usuario autenticado para registrar la operación.");
            }

            return Result.Success();
        }

        public async Task RecordAsync(
            AuditActor actor,
            AuditAction action,
            string tableName,
            long recordId,
            object? previousData,
            object? newData,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(actor);

            var actorValidation = ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                logger.LogWarning("No se registró auditoría para {TableName} {RecordId}: actor inválido.", tableName, recordId);
                return;
            }

            string? previousDataJson = null;
            if (previousData != null)
            {
                previousDataJson = JsonSerializer.Serialize(previousData);
            }

            string? newDataJson = null;
            if (newData != null)
            {
                newDataJson = JsonSerializer.Serialize(newData);
            }

            var auditResult = await registerAuditEntryUseCase.ExecuteAsync(
                new AuditEntry
                {
                    UserId = actor.UserId,
                    Username = actor.Username,
                    Action = action,
                    TableName = tableName,
                    RecordId = recordId,
                    IpAddress = actor.IpAddress,
                    UserAgent = actor.UserAgent,
                    PreviousDataJson = previousDataJson,
                    NewDataJson = newDataJson,
                    Timestamp = DateTime.UtcNow
                },
                cancellationToken);

            if (auditResult.IsFailure)
            {
                logger.LogWarning(
                    "No se pudo registrar auditoría para {TableName} {RecordId}: {Message}",
                    tableName,
                    recordId,
                    auditResult.ErrorMessage);
            }
        }
    }
}
