using Mercadito.src.users.application.models;
using Shared.Domain;

namespace Mercadito.src.users.application.ports.input
{
    public interface IRequestPasswordResetUseCase
    {
        Task<Result<bool>> ExecuteAsync(RequestPasswordResetDto request, CancellationToken cancellationToken = default);
    }
}
