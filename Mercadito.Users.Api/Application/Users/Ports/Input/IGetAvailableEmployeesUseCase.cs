using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Users.Ports.Input
{
    public interface IGetAvailableEmployeesUseCase
    {
        Task<Result<IReadOnlyList<AvailableEmployeeOption>>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
