using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Application.Users.Ports.Input;
using Mercadito.Users.Api.Application.Users.Ports.Output;
using Mercadito.Users.Api.Application.Users.Validation;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Users.UseCases
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
