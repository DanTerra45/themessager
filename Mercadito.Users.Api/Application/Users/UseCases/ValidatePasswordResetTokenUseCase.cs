using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Application.Users.Ports.Input;
using Mercadito.Users.Api.Application.Users.Ports.Output;
using Mercadito.Users.Api.Domain.Shared;
using Mercadito.Users.Api.Domain.Shared.Validation;

namespace Mercadito.Users.Api.Application.Users.UseCases
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
