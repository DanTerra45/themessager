using System.Text.Json;
using Mercadito.src.audit.application.services;
using Mercadito.src.audit.domain.entities;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Shared.Domain;

namespace Mercadito.src.users.application.use_cases
{
        public sealed class DeactivateUserUseCase : IDeactivateUserUseCase
        {
        private readonly IUserRepository _userRepository;
        private readonly IAuditTrailService _auditTrailService;

        public DeactivateUserUseCase(
            IUserRepository userRepository,
            IAuditTrailService auditTrailService)
        {
            _userRepository = userRepository;
            _auditTrailService = auditTrailService;
        }

        public async Task<Result<bool>> ExecuteAsync(long userId, AuditActor actor, CancellationToken cancellationToken = default)
        {
            var actorValidation = _auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return Result<bool>.Failure(actorValidation.ErrorMessage);
            }

            if (userId <= 0)
            {
                return Result<bool>.Failure("El usuario es inválido.");
            }

            if (userId == actor.UserId)
            {
                return Result<bool>.Failure("No puedes dar de baja tu propio usuario.");
            }

            var previousUser = await _userRepository.GetActiveByIdAsync(userId, cancellationToken);
            if (previousUser == null)
            {
                return Result<bool>.Failure("El usuario no existe o ya está inactivo.");
            }

            if (previousUser.Role == domain.entities.UserRole.Admin)
            {
                return Result<bool>.Failure("No se puede dar de baja un usuario administrador.");
            }

            var wasDeactivated = await _userRepository.DeactivateAsync(userId, cancellationToken);
            if (!wasDeactivated)
            {
                return Result<bool>.Failure("El usuario no existe o ya está inactivo.");
            }

            await _auditTrailService.RecordAsync(
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

            return Result<bool>.Success(true);
        }
    }
}
