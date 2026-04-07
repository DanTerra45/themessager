using Mercadito.src.users.application.models;
using Mercadito.src.audit.domain.entities;
using Shared.Domain;

namespace Mercadito.src.users.application.ports.input
{
    public interface IResetUserPasswordUseCase
    {
        Task<Result<bool>> ExecuteAsync(ResetUserPasswordDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
