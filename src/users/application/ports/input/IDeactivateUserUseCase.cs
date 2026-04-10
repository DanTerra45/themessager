using Mercadito.src.audit.domain.entities;
using Mercadito.src.shared.domain;

namespace Mercadito.src.users.application.ports.input
{
    public interface IDeactivateUserUseCase
    {
        Task<Result<bool>> ExecuteAsync(long userId, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
