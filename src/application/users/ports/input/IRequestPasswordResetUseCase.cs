using Mercadito.src.application.users.models;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.users.ports.input
{
    public interface IRequestPasswordResetUseCase
    {
        Task<Result<bool>> ExecuteAsync(RequestPasswordResetDto request, CancellationToken cancellationToken = default);
    }
}
