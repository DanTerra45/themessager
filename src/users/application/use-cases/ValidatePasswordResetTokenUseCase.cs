using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.shared.domain;
using Mercadito.src.shared.domain.validation;

namespace Mercadito.src.users.application.usecases
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
