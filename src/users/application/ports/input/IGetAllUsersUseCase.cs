using Mercadito.src.users.application.models;
using Shared.Domain;

namespace Mercadito.src.users.application.ports.input
{
    public interface IGetAllUsersUseCase
    {
        Task<Result<IReadOnlyList<UserListItem>>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
