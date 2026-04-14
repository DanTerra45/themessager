using Mercadito.src.application.users.models;
using Mercadito.src.application.users.ports.input;
using Mercadito.src.application.users.ports.output;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.users.usecases
{
    public sealed class GetAvailableEmployeesUseCase(IUserRepository userRepository) : IGetAvailableEmployeesUseCase
    {
        public async Task<Result<IReadOnlyList<AvailableEmployeeOption>>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var employees = await userRepository.GetAvailableEmployeesAsync(cancellationToken);
            return Result.Success(employees);
        }
    }
}
