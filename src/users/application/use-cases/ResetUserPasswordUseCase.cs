using Mercadito.src.audit.application.services;
using Mercadito.src.audit.domain.entities;
using Mercadito.src.notifications.application.models;
using Mercadito.src.notifications.application.exceptions;
using Mercadito.src.notifications.application.ports.output;
using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.users.application.validation;
using Shared.Domain;

namespace Mercadito.src.users.application.use_cases
{
    public sealed class ResetUserPasswordUseCase : IResetUserPasswordUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IResetUserPasswordValidator _validator;
        private readonly IEmailSender _emailSender;
        private readonly IAuditTrailService _auditTrailService;
        private readonly ILogger<ResetUserPasswordUseCase> _logger;

        public ResetUserPasswordUseCase(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IResetUserPasswordValidator validator,
            IEmailSender emailSender,
            IAuditTrailService auditTrailService,
            ILogger<ResetUserPasswordUseCase> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _validator = validator;
            _emailSender = emailSender;
            _auditTrailService = auditTrailService;
            _logger = logger;
        }

        public async Task<Result<bool>> ExecuteAsync(ResetUserPasswordDto request, AuditActor actor, CancellationToken cancellationToken = default)
        {
            var actorValidation = _auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return Result<bool>.Failure(actorValidation.ErrorMessage);
            }

            var validationResult = _validator.Validate(request);
            if (validationResult.IsFailure)
            {
                return Result<bool>.Failure(validationResult.Errors);
            }

            try
            {
                var normalized = validationResult.Value;
                var user = await _userRepository.GetActiveByIdAsync(normalized.UserId, cancellationToken);
                if (user == null)
                {
                    return Result<bool>.Failure("El usuario no existe o no está activo.");
                }

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    _emailSender.EnsureAvailable();
                }

                var passwordHash = _passwordHasher.Hash(normalized.Password);
                var notificationEmail = BuildPasswordResetEmail(user);
                var wasUpdated = await _userRepository.ResetPasswordAndQueueNotificationAsync(
                    normalized.UserId,
                    passwordHash,
                    notificationEmail,
                    cancellationToken);

                if (!wasUpdated)
                {
                    return Result<bool>.Failure("El usuario no existe o no está activo.");
                }

                await _auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Update,
                    "usuarios",
                    normalized.UserId,
                        new { PasswordReset = false },
                        new { PasswordReset = true },
                        cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (EmailDeliveryException deliveryException)
            {
                _logger.LogWarning(deliveryException, "No se pudo notificar el restablecimiento administrativo para el usuario {Username}.", request.Username);
                return Result<bool>.Failure("La contraseña se actualizó, pero no se pudo enviar la notificación por correo.");
            }
            catch (BusinessValidationException validationException)
            {
                return validationException.Errors.Count > 0
                    ? Result<bool>.Failure(validationException.Errors)
                    : Result<bool>.Failure(validationException.Message);
            }
        }

        private static EmailMessage? BuildPasswordResetEmail(domain.entities.User user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return null;
            }

            return new EmailMessage
            {
                ToAddress = user.Email,
                Subject = "Tu contraseña de Mercadito fue restablecida",
                PlainTextBody =
                    $"Hola {user.Username},\n\n" +
                    "Un administrador restableció tu contraseña de acceso a Mercadito.\n" +
                    "Solicita la nueva credencial directamente a administración si aún no la recibiste.\n",
                HtmlBody =
                    $"<p>Hola <strong>{user.Username}</strong>,</p>" +
                    "<p>Un administrador restableció tu contraseña de acceso a Mercadito.</p>" +
                    "<p>Solicita la nueva credencial directamente a administración si aún no la recibiste.</p>"
            };
        }
    }
}
