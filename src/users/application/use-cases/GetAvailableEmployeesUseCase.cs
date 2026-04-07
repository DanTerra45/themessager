using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Shared.Domain;

namespace Mercadito.src.users.application.use_cases
{
    public sealed class GetAvailableEmployeesUseCase : IGetAvailableEmployeesUseCase
    {
        private readonly IUserRepository _userRepository;

        public GetAvailableEmployeesUseCase(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<IReadOnlyList<AvailableEmployeeOption>>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var employees = await _userRepository.GetAvailableEmployeesAsync(cancellationToken);
            return Result<IReadOnlyList<AvailableEmployeeOption>>.Success(employees);
        }
    }
}
