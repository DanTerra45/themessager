using Mercadito.src.users.application.models;
using Shared.Domain;

namespace Mercadito.src.users.application.ports.input
{
    public interface IGetAvailableEmployeesUseCase
    {
        Task<Result<IReadOnlyList<AvailableEmployeeOption>>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
