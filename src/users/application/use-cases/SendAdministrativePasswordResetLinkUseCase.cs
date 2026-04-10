using Mercadito.src.audit.application.services;
using Mercadito.src.audit.domain.entities;
using Mercadito.src.notifications.application.exceptions;
using Mercadito.src.notifications.application.models;
using Mercadito.src.notifications.application.ports.output;
using Mercadito.src.shared.domain;
using Mercadito.src.shared.domain.exceptions;
using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.users.application.validation;
using Mercadito.src.users.application.emails;

namespace Mercadito.src.users.application.usecases
{
    public sealed class SendAdministrativePasswordResetLinkUseCase(
        IUserRepository userRepository,
        IUserAccessWorkflowRepository userAccessWorkflowRepository,
        IPasswordHasher passwordHasher,
        ISendAdministrativePasswordResetLinkValidator validator,
        IEmailSender emailSender,
        IAuditTrailService auditTrailService,
        ILogger<SendAdministrativePasswordResetLinkUseCase> logger) : ISendAdministrativePasswordResetLinkUseCase
    {
        public async Task<Result<bool>> ExecuteAsync(SendAdministrativePasswordResetLinkDto request, AuditActor actor, CancellationToken cancellationToken = default)
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

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    return Result.Failure<bool>("El usuario no tiene un correo asociado para enviar el enlace.");
                }

                emailSender.EnsureAvailable();

                var plainToken = PasswordResetTokenCodec.CreatePlainToken();
                var tokenHash = PasswordResetTokenCodec.HashToken(plainToken);
                var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);
                var currentUtc = DateTime.UtcNow;
                var replacementPasswordHash = passwordHasher.Hash(PasswordResetTokenCodec.CreatePlainToken());
                var resetLink = BuildResetLink(normalized.ResetUrlBase, plainToken);
                var emailMessage = BuildAdministrativeResetEmail(user.Username, user.Email, resetLink);

                var wasUpdated = await userAccessWorkflowRepository.BeginAdministrativePasswordResetAsync(
                    normalized.UserId,
                    replacementPasswordHash,
                    tokenHash,
                    expiresAtUtc,
                    currentUtc,
                    emailMessage,
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
                    new { AdministrativePasswordResetLinkSent = false, PasswordInvalidated = false },
                    new { AdministrativePasswordResetLinkSent = true, PasswordInvalidated = true },
                    cancellationToken);

                return Result.Success(true);
            }
            catch (EmailDeliveryException deliveryException)
            {
                logger.LogWarning(deliveryException, "No se pudo iniciar el restablecimiento administrativo por correo para el usuario {Username}.", request.Username);
                return Result.Failure<bool>("No se pudo preparar el correo de restablecimiento. Revisa la configuración SMTP.");
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

        private static string BuildResetLink(string resetUrlBase, string token)
        {
            var separator = "/";
            if (resetUrlBase.EndsWith('/'))
            {
                separator = string.Empty;
            }

            return $"{resetUrlBase}{separator}{Uri.EscapeDataString(token)}";
        }

        private static EmailMessage BuildAdministrativeResetEmail(string username, string email, string resetLink)
        {
            return new EmailMessage
            {
                ToAddress = email,
                Subject = "Un administrador reinició tu acceso a Mercadito",
                PlainTextBody =
                    $"Hola {username},\n\n" +
                    "Un administrador reinició tu acceso a Mercadito.\n" +
                    "Tu contraseña anterior dejó de ser válida.\n" +
                    "Abre el siguiente enlace para definir una nueva contraseña:\n" +
                    $"{resetLink}\n\n" +
                    "El enlace vence en 30 minutos.\n",
                HtmlBody = UserAccessEmailTemplate.BuildAdministrativeResetHtml(username, resetLink)
            };
        }
    }
}
