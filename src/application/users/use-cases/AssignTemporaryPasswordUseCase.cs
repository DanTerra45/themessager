using Mercadito.src.application.audit.services;
using Mercadito.src.domain.audit.entities;
using Mercadito.src.domain.shared;
using Mercadito.src.domain.shared.exceptions;
using Mercadito.src.application.users.models;
using Mercadito.src.application.users.ports.input;
using Mercadito.src.application.users.ports.output;
using Mercadito.src.application.users.validation;

namespace Mercadito.src.application.users.usecases
{
    public sealed class AssignTemporaryPasswordUseCase(
        IUserRepository userRepository,
        IUserAccessWorkflowRepository userAccessWorkflowRepository,
        IPasswordHasher passwordHasher,
        IAssignTemporaryPasswordValidator validator,
        IAuditTrailService auditTrailService) : IAssignTemporaryPasswordUseCase
    {
        public async Task<Result<bool>> ExecuteAsync(AssignTemporaryPasswordDto request, AuditActor actor, CancellationToken cancellationToken = default)
        {
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
                var normalized = validationResult.Value;
                var user = await userRepository.GetActiveByIdAsync(normalized.UserId, cancellationToken);
                if (user == null)
                {
                    return Result.Failure<bool>("El usuario no existe o no está activo.");
                }

                var passwordHash = passwordHasher.Hash(normalized.Password);
                var wasUpdated = await userAccessWorkflowRepository.SetTemporaryPasswordAsync(
                    normalized.UserId,
                    passwordHash,
                    DateTime.UtcNow,
                    cancellationToken);

                if (!wasUpdated)
                {
                    return Result.Failure<bool>("El usuario no existe o no está activo.");
                }

                await auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Update,
                    "usuarios",
                    normalized.UserId,
                    new { TemporaryPasswordAssigned = false, MustChangePassword = false },
                    new { TemporaryPasswordAssigned = true, MustChangePassword = true },
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
