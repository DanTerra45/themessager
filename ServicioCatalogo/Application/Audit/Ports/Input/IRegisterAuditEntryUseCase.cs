using ServicioCatalogo.Domain.Audit.Entities;
using ServicioCatalogo.Domain.Shared;

namespace ServicioCatalogo.Application.Audit.Ports.Input
{
    public interface IRegisterAuditEntryUseCase
    {
        Task<Result> ExecuteAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    }
}

