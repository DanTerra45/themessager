using Mercadito.src.application.users.models;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.users.ports.input
{
    public interface IGetAllUsersUseCase
    {
        Task<Result<IReadOnlyList<UserListItem>>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
