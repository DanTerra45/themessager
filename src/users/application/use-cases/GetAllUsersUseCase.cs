using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Shared.Domain;

namespace Mercadito.src.users.application.use_cases
{
    public sealed class GetAllUsersUseCase : IGetAllUsersUseCase
    {
        private readonly IUserRepository _userRepository;

        public GetAllUsersUseCase(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<IReadOnlyList<UserListItem>>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var users = await _userRepository.GetAllActiveAsync(cancellationToken);
            return Result<IReadOnlyList<UserListItem>>.Success(users);
        }
    }
}
