using Mercadito.Users.Api.Domain.Audit.Entities;

namespace Mercadito.Users.Api.Application.Audit.Ports.Output
{
    public interface IAuditRepository
    {
        Task RegisterAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    }
}
