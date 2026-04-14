using Mercadito.src.domain.audit.entities;
using Mercadito.src.domain.shared;
using Mercadito.src.application.users.models;

namespace Mercadito.src.application.users.ports.input
{
    public interface ISendAdministrativePasswordResetLinkUseCase
    {
        Task<Result<bool>> ExecuteAsync(SendAdministrativePasswordResetLinkDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
