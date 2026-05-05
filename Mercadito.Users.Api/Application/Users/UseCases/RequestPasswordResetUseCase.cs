using Mercadito.Users.Api.Application.Notifications.Models;
using Mercadito.Users.Api.Application.Notifications.Exceptions;
using Mercadito.Users.Api.Application.Notifications.Ports.Output;
using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Application.Users.Ports.Input;
using Mercadito.Users.Api.Application.Users.Ports.Output;
using Mercadito.Users.Api.Application.Users.Validation;
using Mercadito.Users.Api.Application.Users.Emails;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Users.UseCases
{
    public sealed class RequestPasswordResetUseCase(
        IUserRepository userRepository,
        IUserAccessWorkflowRepository userAccessWorkflowRepository,
        IRequestPasswordResetValidator validator,
        IEmailSender emailSender,
        ILogger<RequestPasswordResetUseCase> logger) : IRequestPasswordResetUseCase
    {
        public async Task<Result<bool>> ExecuteAsync(RequestPasswordResetDto request, CancellationToken cancellationToken = default)
        {
            var validationResult = validator.Validate(request);
            if (validationResult.IsFailure)
            {
                return Result.Failure<bool>(validationResult.Errors);
            }

            var normalized = validationResult.Value;
            var user = await userRepository.GetActiveByUsernameOrEmailAsync(normalized.Identifier, cancellationToken);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                return Result.Success(true);
            }

            emailSender.EnsureAvailable();

            var plainToken = PasswordResetTokenCodec.CreatePlainToken();
            var tokenHash = PasswordResetTokenCodec.HashToken(plainToken);
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);
            var currentUtc = DateTime.UtcNow;

            var resetLink = BuildResetLink(normalized.ResetUrlBase, plainToken);
            var emailMessage = new EmailMessage
            {
                ToAddress = user.Email,
                Subject = "Restablece tu contraseña de Mercadito",
                PlainTextBody =
                    $"Hola {user.Username},\n\n" +
                    "Recibimos una solicitud para restablecer tu contraseña de Mercadito.\n" +
                    $"Abre este enlace para continuar:\n{resetLink}\n\n" +
                    "El enlace vence en 30 minutos.\n" +
                    "Si no solicitaste este cambio, puedes ignorar este mensaje.\n",
                HtmlBody = UserAccessEmailTemplate.BuildPasswordResetHtml(user.Username, resetLink)
            };

            try
            {
                await userAccessWorkflowRepository.CreatePasswordResetTokenAndQueueEmailAsync(
                    user.Id,
                    tokenHash,
                    expiresAtUtc,
                    currentUtc,
                    emailMessage,
                    cancellationToken);

                return Result.Success(true);
            }
            catch (EmailDeliveryException deliveryException)
            {
                logger.LogWarning(deliveryException, "No se pudo enviar el correo de restablecimiento para el usuario {Username}.", user.Username);
                return Result.Failure<bool>("No se pudo enviar el correo de restablecimiento. Revisa la configuración SMTP.");
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
    }
}
