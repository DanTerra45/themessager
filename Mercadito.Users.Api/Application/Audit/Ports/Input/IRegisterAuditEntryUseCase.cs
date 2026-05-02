using Mercadito.Users.Api.Domain.Audit.Entities;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Audit.Ports.Input
{
    public interface IRegisterAuditEntryUseCase
    {
        Task<Result> ExecuteAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    }
}
