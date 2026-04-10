using Mercadito.src.audit.domain.entities;
using Mercadito.src.shared.domain;

namespace Mercadito.src.audit.application.ports.input
{
    public interface IRegisterAuditEntryUseCase
    {
        Task<Result> ExecuteAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    }
}
