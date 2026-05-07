using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Users.Ports.Input
{
    public interface IRequestPasswordResetUseCase
    {
        Task<Result<bool>> ExecuteAsync(RequestPasswordResetDto request, CancellationToken cancellationToken = default);
    }
}
