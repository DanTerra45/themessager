using ServicioCatalogo.Domain.Audit.Entities;

namespace ServicioCatalogo.Application.Audit.Ports.Output
{
    public interface IAuditRepository
    {
        Task RegisterAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    }
}

