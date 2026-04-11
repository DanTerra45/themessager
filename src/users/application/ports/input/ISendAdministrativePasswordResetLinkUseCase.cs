using Mercadito.src.audit.domain.entities;
using Mercadito.src.shared.domain;
using Mercadito.src.users.application.models;

namespace Mercadito.src.users.application.ports.input
{
    public interface ISendAdministrativePasswordResetLinkUseCase
    {
        Task<Result<bool>> ExecuteAsync(SendAdministrativePasswordResetLinkDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
