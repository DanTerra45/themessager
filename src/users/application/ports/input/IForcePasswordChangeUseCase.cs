using Mercadito.src.audit.domain.entities;
using Mercadito.src.shared.domain;
using Mercadito.src.users.application.models;

namespace Mercadito.src.users.application.ports.input
{
    public interface IForcePasswordChangeUseCase
    {
        Task<Result<bool>> ExecuteAsync(long userId, ForcePasswordChangeDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
