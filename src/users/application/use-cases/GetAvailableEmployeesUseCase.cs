using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.shared.domain;

namespace Mercadito.src.users.application.usecases
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
