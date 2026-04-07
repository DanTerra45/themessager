using Mercadito.src.users.application.models;
using Shared.Domain;

namespace Mercadito.src.users.application.ports.input
{
    public interface IAuthenticateUserUseCase
    {
        Task<Result<AuthenticatedUser>> ExecuteAsync(LoginUserCommand command, CancellationToken cancellationToken = default);
    }
}
