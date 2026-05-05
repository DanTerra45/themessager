using Mercadito.Users.Api.Application.Audit.Services;
using Mercadito.Users.Api.Domain.Audit.Entities;
using Mercadito.Users.Api.Domain.Shared;
using Mercadito.Users.Api.Domain.Shared.Exceptions;
using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Application.Users.Ports.Input;
using Mercadito.Users.Api.Application.Users.Ports.Output;
using Mercadito.Users.Api.Application.Users.Validation;

namespace Mercadito.Users.Api.Application.Users.UseCases
{
    public sealed class ForcePasswordChangeUseCase(
        IUserRepository userRepository,
        IUserAccessWorkflowRepository userAccessWorkflowRepository,
        IPasswordHasher passwordHasher,
        IForcePasswordChangeValidator validator,
        IAuditTrailService auditTrailService) : IForcePasswordChangeUseCase
    {
        public async Task<Result<bool>> ExecuteAsync(long userId, ForcePasswordChangeDto request, AuditActor actor, CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(actor);

            var actorValidation = auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return Result.Failure<bool>(actorValidation.ErrorMessage);
            }

            var validationResult = validator.Validate(request);
            if (validationResult.IsFailure)
            {
                return Result.Failure<bool>(validationResult.Errors);
            }

            try
            {
                var user = await userRepository.GetActiveByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    return Result.Failure<bool>("El usuario no existe o no está activo.");
                }

                if (!user.MustChangePassword)
                {
                    return Result.Failure<bool>("El usuario no tiene un cambio obligatorio de contraseña pendiente.");
                }

                var normalized = validationResult.Value;
                var passwordHash = passwordHasher.Hash(normalized.Password);
                var wasUpdated = await userAccessWorkflowRepository.CompleteForcedPasswordChangeAsync(
                    userId,
                    passwordHash,
                    DateTime.UtcNow,
                    cancellationToken);

                if (!wasUpdated)
                {
                    return Result.Failure<bool>("No se pudo actualizar la contraseña.");
                }

                await auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Update,
                    "usuarios",
                    userId,
                    new { MustChangePassword = true },
                    new { MustChangePassword = false },
                    cancellationToken);

                return Result.Success(true);
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure<bool>(validationException.Errors);
                }

                return Result.Failure<bool>(validationException.Message);
            }
        }
    }
}
