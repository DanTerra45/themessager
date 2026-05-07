using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Users.Ports.Input
{
    public interface IAuthenticateUserUseCase
    {
        Task<Result<AuthenticatedUser>> ExecuteAsync(LoginUserCommand command, CancellationToken cancellationToken = default);
    }
}
