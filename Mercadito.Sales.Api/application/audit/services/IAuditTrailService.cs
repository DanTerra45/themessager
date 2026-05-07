using Mercadito.Sales.Api.Domain.Audit.Entities;
using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Audit.Services
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
