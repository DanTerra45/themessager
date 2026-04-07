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
    public sealed class RequestPasswordResetUseCase : IRequestPasswordResetUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IRequestPasswordResetValidator _validator;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<RequestPasswordResetUseCase> _logger;

        public RequestPasswordResetUseCase(
            IUserRepository userRepository,
            IRequestPasswordResetValidator validator,
            IEmailSender emailSender,
            ILogger<RequestPasswordResetUseCase> logger)
        {
            _userRepository = userRepository;
            _validator = validator;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<Result<bool>> ExecuteAsync(RequestPasswordResetDto request, CancellationToken cancellationToken = default)
        {
            var validationResult = _validator.Validate(request);
            if (validationResult.IsFailure)
            {
                return Result<bool>.Failure(validationResult.Errors);
            }

            var normalized = validationResult.Value;
            var user = await _userRepository.GetActiveByUsernameOrEmailAsync(normalized.Identifier, cancellationToken);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                return Result<bool>.Success(true);
            }

            _emailSender.EnsureAvailable();

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
                HtmlBody =
                    $"<p>Hola <strong>{user.Username}</strong>,</p>" +
                    "<p>Recibimos una solicitud para restablecer tu contraseña de Mercadito.</p>" +
                    $"<p><a href=\"{resetLink}\">Haz clic aquí para restablecer tu contraseña</a></p>" +
                    "<p>El enlace vence en 30 minutos.</p>" +
                    "<p>Si no solicitaste este cambio, puedes ignorar este mensaje.</p>"
            };

            try
            {
                await _userRepository.CreatePasswordResetTokenAndQueueEmailAsync(
                    user.Id,
                    tokenHash,
                    expiresAtUtc,
                    currentUtc,
                    emailMessage,
                    cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (EmailDeliveryException deliveryException)
            {
                _logger.LogWarning(deliveryException, "No se pudo enviar el correo de restablecimiento para el usuario {Username}.", user.Username);
                return Result<bool>.Failure("No se pudo enviar el correo de restablecimiento. Revisa la configuración SMTP.");
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
