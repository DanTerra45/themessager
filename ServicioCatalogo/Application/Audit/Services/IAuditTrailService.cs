using ServicioCatalogo.Domain.Audit.Entities;
using ServicioCatalogo.Domain.Shared;

namespace ServicioCatalogo.Application.Audit.Services
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

