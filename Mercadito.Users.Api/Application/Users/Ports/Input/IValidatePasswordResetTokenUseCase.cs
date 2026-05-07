using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Users.Ports.Input
{
    public interface IValidatePasswordResetTokenUseCase
    {
        Task<Result<PasswordResetTokenInfo>> ExecuteAsync(string token, CancellationToken cancellationToken = default);
    }
}
