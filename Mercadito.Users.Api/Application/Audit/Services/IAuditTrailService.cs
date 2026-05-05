using Mercadito.Users.Api.Domain.Audit.Entities;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Audit.Services
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
