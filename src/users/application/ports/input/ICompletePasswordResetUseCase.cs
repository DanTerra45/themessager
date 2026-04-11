using Mercadito.src.users.application.models;
using Mercadito.src.shared.domain;

namespace Mercadito.src.users.application.ports.input
{
    public interface ICompletePasswordResetUseCase
    {
        Task<Result<bool>> ExecuteAsync(CompletePasswordResetDto request, CancellationToken cancellationToken = default);
    }
}
