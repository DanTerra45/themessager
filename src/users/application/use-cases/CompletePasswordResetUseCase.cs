using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.users.application.validation;
using Mercadito.src.shared.domain;

namespace Mercadito.src.users.application.usecases
{
    public sealed class CompletePasswordResetUseCase(
        IUserAccessWorkflowRepository userAccessWorkflowRepository,
        IPasswordHasher passwordHasher,
        ICompletePasswordResetValidator validator) : ICompletePasswordResetUseCase
    {
        public async Task<Result<bool>> ExecuteAsync(CompletePasswordResetDto request, CancellationToken cancellationToken = default)
        {
            var validationResult = validator.Validate(request);
            if (validationResult.IsFailure)
            {
                return Result.Failure<bool>(validationResult.Errors);
            }

            var normalized = validationResult.Value;
            var tokenHash = PasswordResetTokenCodec.HashToken(normalized.Token);
            var passwordHash = passwordHasher.Hash(normalized.Password);
            var wasUpdated = await userAccessWorkflowRepository.ResetPasswordByTokenAsync(tokenHash, passwordHash, DateTime.UtcNow, cancellationToken);

            if (!wasUpdated)
            {
                return Result.Failure<bool>("El enlace de restablecimiento es inválido o ya venció.");
            }

            return Result.Success(true);
        }
    }
}
