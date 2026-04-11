using Mercadito.src.audit.application.services;
using Mercadito.src.audit.domain.entities;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.shared.domain;

namespace Mercadito.src.users.application.usecases
{
    public sealed class DeactivateUserUseCase(
        IUserRepository userRepository,
        IAuditTrailService auditTrailService) : IDeactivateUserUseCase
    {

        public async Task<Result<bool>> ExecuteAsync(long userId, AuditActor actor, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(actor);

            var actorValidation = auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return Result.Failure<bool>(actorValidation.ErrorMessage);
            }

            if (userId <= 0)
            {
                return Result.Failure<bool>("El usuario es inválido.");
            }

            if (userId == actor.UserId)
            {
                return Result.Failure<bool>("No puedes dar de baja tu propio usuario.");
            }

            var previousUser = await userRepository.GetActiveByIdAsync(userId, cancellationToken);
            if (previousUser == null)
            {
                return Result.Failure<bool>("El usuario no existe o ya está inactivo.");
            }

            if (previousUser.Role == domain.entities.UserRole.Admin)
            {
                return Result.Failure<bool>("No se puede dar de baja un usuario administrador.");
            }

            var wasDeactivated = await userRepository.DeactivateAsync(userId, cancellationToken);
            if (!wasDeactivated)
            {
                return Result.Failure<bool>("El usuario no existe o ya está inactivo.");
            }

            await auditTrailService.RecordAsync(
                actor,
                AuditAction.Delete,
                "usuarios",
                userId,
                new
                {
                    previousUser.Username,
                    previousUser.Email,
                    Role = previousUser.Role.ToString(),
                    previousUser.EmployeeId,
                    previousUser.State
                },
                new { Estado = "I" },
                cancellationToken);

            return Result.Success(true);
        }
    }
}
