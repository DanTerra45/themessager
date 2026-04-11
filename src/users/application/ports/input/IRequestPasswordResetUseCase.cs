using Mercadito.src.users.application.models;
using Mercadito.src.shared.domain;

namespace Mercadito.src.users.application.ports.input
{
    public interface IRequestPasswordResetUseCase
    {
        Task<Result<bool>> ExecuteAsync(RequestPasswordResetDto request, CancellationToken cancellationToken = default);
    }
}
