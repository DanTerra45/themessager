using Mercadito.src.domain.audit.entities;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.users.ports.input
{
    public interface IDeactivateUserUseCase
    {
        Task<Result<bool>> ExecuteAsync(long userId, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
