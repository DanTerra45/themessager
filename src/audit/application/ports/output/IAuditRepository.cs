using Mercadito.src.audit.domain.entities;

namespace Mercadito.src.audit.application.ports.output
{
    public interface IAuditRepository
    {
        Task RegisterAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    }
}
