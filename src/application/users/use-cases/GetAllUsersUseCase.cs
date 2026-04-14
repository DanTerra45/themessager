using Mercadito.src.application.users.models;
using Mercadito.src.application.users.ports.input;
using Mercadito.src.application.users.ports.output;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.users.usecases
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
