using Mercadito.src.application.users.models;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.users.ports.input
{
    public interface IGetAvailableEmployeesUseCase
    {
        Task<Result<IReadOnlyList<AvailableEmployeeOption>>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
