using Mercadito.src.domain.audit.entities;
using Mercadito.src.domain.shared;
using Mercadito.src.application.users.models;

namespace Mercadito.src.application.users.ports.input
{
    public interface IAssignTemporaryPasswordUseCase
    {
        Task<Result<bool>> ExecuteAsync(AssignTemporaryPasswordDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
