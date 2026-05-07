using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Application.Users.Ports.Input;
using Mercadito.Users.Api.Application.Users.Ports.Output;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Users.UseCases
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
