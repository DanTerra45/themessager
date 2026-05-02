using Mercadito.src.domain.audit.entities;

namespace Mercadito.src.application.audit.ports.output
{
    public interface IAuditRepository
    {
        Task RegisterAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    }
}
