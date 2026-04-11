using Mercadito.src.audit.domain.entities;
using Mercadito.src.shared.domain;
using Mercadito.src.users.application.models;

namespace Mercadito.src.users.application.ports.input
{
    public interface IAssignTemporaryPasswordUseCase
    {
        Task<Result<bool>> ExecuteAsync(AssignTemporaryPasswordDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
