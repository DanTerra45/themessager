using System.Text.Json;
using Mercadito.src.audit.application.ports.input;
using Mercadito.src.audit.domain.entities;
using Shared.Domain;

namespace Mercadito.src.audit.application.services
{
    public sealed class AuditTrailService : IAuditTrailService
    {
        private readonly IRegisterAuditEntryUseCase _registerAuditEntryUseCase;
        private readonly ILogger<AuditTrailService> _logger;

        public AuditTrailService(
            IRegisterAuditEntryUseCase registerAuditEntryUseCase,
            ILogger<AuditTrailService> logger)
        {
            _registerAuditEntryUseCase = registerAuditEntryUseCase;
            _logger = logger;
        }

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
                _logger.LogWarning("No se registró auditoría para {TableName} {RecordId}: actor inválido.", tableName, recordId);
                return;
            }

            var auditResult = await _registerAuditEntryUseCase.ExecuteAsync(
                new AuditEntry
                {
                    UserId = actor.UserId,
                    Username = actor.Username,
                    Action = action,
                    TableName = tableName,
                    RecordId = recordId,
                    IpAddress = actor.IpAddress,
                    UserAgent = actor.UserAgent,
                    PreviousDataJson = previousData == null ? null : JsonSerializer.Serialize(previousData),
                    NewDataJson = newData == null ? null : JsonSerializer.Serialize(newData),
                    Timestamp = DateTime.UtcNow
                },
                cancellationToken);

            if (auditResult.IsFailure)
            {
                _logger.LogWarning(
                    "No se pudo registrar auditoría para {TableName} {RecordId}: {Message}",
                    tableName,
                    recordId,
                    auditResult.ErrorMessage);
            }
        }
    }
}
