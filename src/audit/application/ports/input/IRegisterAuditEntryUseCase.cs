using Mercadito.src.audit.domain.entities;
using Shared.Domain;

namespace Mercadito.src.audit.application.ports.input
{
    public interface IRegisterAuditEntryUseCase
    {
        Task<Result> ExecuteAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    }
}
