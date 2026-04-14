using Mercadito.src.application.users.models;
using Mercadito.src.domain.audit.entities;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.users.ports.input
{
    public interface IRegisterUserUseCase
    {
        Task<Result<long>> ExecuteAsync(CreateUserDto user, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
