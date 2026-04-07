using Mercadito.src.audit.domain.entities;
using Shared.Domain;

namespace Mercadito.src.audit.application.services
{
    public interface IAuditTrailService
    {
        Result ValidateActor(AuditActor actor);
        Task RecordAsync(
            AuditActor actor,
            AuditAction action,
            string tableName,
            long recordId,
            object? previousData,
            object? newData,
            CancellationToken cancellationToken = default);
    }
}
