using Mercadito.src.domain.audit.entities;
using Mercadito.src.domain.shared;
using Mercadito.src.application.users.models;

namespace Mercadito.src.application.users.ports.input
{
    public interface IForcePasswordChangeUseCase
    {
        Task<Result<bool>> ExecuteAsync(long userId, ForcePasswordChangeDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
