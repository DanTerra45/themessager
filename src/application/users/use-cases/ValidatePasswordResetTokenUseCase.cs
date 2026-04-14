using Mercadito.src.application.users.models;
using Mercadito.src.application.users.ports.input;
using Mercadito.src.application.users.ports.output;
using Mercadito.src.domain.shared;
using Mercadito.src.domain.shared.validation;

namespace Mercadito.src.application.users.usecases
{
    public sealed class ValidatePasswordResetTokenUseCase(IUserAccessWorkflowRepository userAccessWorkflowRepository) : IValidatePasswordResetTokenUseCase
    {
        public async Task<Result<PasswordResetTokenInfo>> ExecuteAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Result.Failure<PasswordResetTokenInfo>("El enlace de restablecimiento es inválido.");
            }

            var tokenHash = PasswordResetTokenCodec.HashToken(ValidationText.NormalizeTrimmed(token));
            var tokenInfo = await userAccessWorkflowRepository.GetValidPasswordResetTokenAsync(tokenHash, DateTime.UtcNow, cancellationToken);
            if (tokenInfo == null)
            {
                return Result.Failure<PasswordResetTokenInfo>("El enlace de restablecimiento es inválido o ya venció.");
            }

            return Result.Success(tokenInfo);
        }
    }
}
