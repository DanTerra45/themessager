using Mercadito.src.domain.audit.entities;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.audit.ports.input
{
    public interface IRegisterAuditEntryUseCase
    {
        Task<Result> ExecuteAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    }
}
