using Mercadito.src.application.users.models;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.users.ports.input
{
    public interface ICompletePasswordResetUseCase
    {
        Task<Result<bool>> ExecuteAsync(CompletePasswordResetDto request, CancellationToken cancellationToken = default);
    }
}
