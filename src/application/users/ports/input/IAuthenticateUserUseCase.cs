using Mercadito.src.application.users.models;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.users.ports.input
{
    public interface IAuthenticateUserUseCase
    {
        Task<Result<AuthenticatedUser>> ExecuteAsync(LoginUserCommand command, CancellationToken cancellationToken = default);
    }
}
