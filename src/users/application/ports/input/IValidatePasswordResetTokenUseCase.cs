using Mercadito.src.users.application.models;
using Mercadito.src.shared.domain;

namespace Mercadito.src.users.application.ports.input
{
    public interface IValidatePasswordResetTokenUseCase
    {
        Task<Result<PasswordResetTokenInfo>> ExecuteAsync(string token, CancellationToken cancellationToken = default);
    }
}
