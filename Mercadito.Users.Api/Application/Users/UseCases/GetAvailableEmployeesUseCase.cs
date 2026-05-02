using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Application.Users.Ports.Input;
using Mercadito.Users.Api.Application.Users.Ports.Output;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Users.UseCases
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
