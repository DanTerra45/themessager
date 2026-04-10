using Mercadito.src.users.application.models;
using Mercadito.src.audit.domain.entities;
using Mercadito.src.shared.domain;

namespace Mercadito.src.users.application.ports.input
{
    public interface IRegisterUserUseCase
    {
        Task<Result<long>> ExecuteAsync(CreateUserDto user, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
