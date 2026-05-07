using Mercadito.Sales.Api.Domain.Audit.Entities;
using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Audit.Ports.Input
{
    public interface IRegisterAuditEntryUseCase
    {
        Task<Result> ExecuteAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    }
}
