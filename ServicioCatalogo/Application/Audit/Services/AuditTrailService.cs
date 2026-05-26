using System.Text.Json;
using ServicioCatalogo.Application.Audit.Ports.Input;
using ServicioCatalogo.Domain.Audit.Entities;
using ServicioCatalogo.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace ServicioCatalogo.Application.Audit.Services
{
    public sealed class AuditTrailService(
        IRegisterAuditEntryUseCase registerAuditEntryUseCase,
        ILogger<AuditTrailService> logger) : IAuditTrailService
    {
        public Result ValidateActor(AuditActor actor)
        {
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

