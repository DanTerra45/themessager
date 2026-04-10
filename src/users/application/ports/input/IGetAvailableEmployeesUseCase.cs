using Mercadito.src.users.application.models;
using Mercadito.src.shared.domain;

namespace Mercadito.src.users.application.ports.input
{
    public interface IGetAvailableEmployeesUseCase
    {
        Task<Result<IReadOnlyList<AvailableEmployeeOption>>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
