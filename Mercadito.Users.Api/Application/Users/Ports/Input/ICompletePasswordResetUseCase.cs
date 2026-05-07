using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Users.Ports.Input
{
    public interface ICompletePasswordResetUseCase
    {
        Task<Result<bool>> ExecuteAsync(CompletePasswordResetDto request, CancellationToken cancellationToken = default);
    }
}
