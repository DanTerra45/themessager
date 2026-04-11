using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.shared.domain;

namespace Mercadito.src.users.application.usecases
{
    public sealed class GetAllUsersUseCase(IUserRepository userRepository) : IGetAllUsersUseCase
    {
        public async Task<Result<IReadOnlyList<UserListItem>>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var users = await userRepository.GetAllActiveAsync(cancellationToken);
            return Result.Success(users);
        }
    }
}
