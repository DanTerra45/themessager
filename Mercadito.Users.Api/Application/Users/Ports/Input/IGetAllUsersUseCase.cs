using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Users.Ports.Input
{
    public interface IGetAllUsersUseCase
    {
        Task<Result<IReadOnlyList<UserListItem>>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
