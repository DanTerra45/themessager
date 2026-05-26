using Mercadito.Sales.Api.Domain.Audit.Entities;

namespace Mercadito.Sales.Api.Application.Audit.Ports.Output
{
    public interface IAuditRepository
    {
        Task RegisterAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    }
}
