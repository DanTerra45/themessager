using Mercadito.src.application.audit.services;
using Mercadito.src.domain.audit.entities;
using Mercadito.src.application.users.ports.input;
using Mercadito.src.application.users.ports.output;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.users.usecases
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

            if (previousUser.Role == src.domain.users.entities.UserRole.Admin)
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
