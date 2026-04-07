using MailKit.Net.Smtp;
using MailKit.Security;
using Mercadito.src.notifications.application.models;
using Mercadito.src.notifications.application.exceptions;
using Mercadito.src.notifications.application.ports.output;
using Mercadito.src.notifications.infrastructure.options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Mercadito.src.notifications.infrastructure.email
{
    public sealed class MailKitEmailSender : IEmailSender
    {
        private readonly SmtpOptions _options;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogger<MailKitEmailSender> _logger;

        public MailKitEmailSender(
            IOptions<SmtpOptions> options,
            IHostEnvironment hostEnvironment,
            ILogger<MailKitEmailSender> logger)
        {
            _options = options.Value;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        public void EnsureAvailable()
        {
            if (!_options.Enabled)
            {
                throw new EmailDeliveryException("El envío de correo está deshabilitado en la configuración actual.");
            }

            if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
            {
                throw new EmailDeliveryException("La configuración SMTP está incompleta.");
            }
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            EnsureAvailable();

            if (string.IsNullOrWhiteSpace(message.ToAddress))
            {
                throw new EmailDeliveryException("No se pudo enviar el correo porque el destinatario no tiene dirección.");
            }

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            email.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
            email.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder
            {
                TextBody = message.PlainTextBody
            };

            if (!string.IsNullOrWhiteSpace(message.HtmlBody))
            {
                bodyBuilder.HtmlBody = message.HtmlBody;
            }

            email.Body = bodyBuilder.ToMessageBody();

            using var smtpClient = new SmtpClient();
            if (_options.AllowInvalidServerCertificate && _hostEnvironment.IsDevelopment())
            {
                smtpClient.ServerCertificateValidationCallback = (_, _, _, _) => true;
            }

            var secureSocketOptions = _options.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto;

            try
            {
                await smtpClient.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, cancellationToken);

                if (!string.IsNullOrWhiteSpace(_options.Username))
                {
                    await smtpClient.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
                }

                await smtpClient.SendAsync(email, cancellationToken);
                await smtpClient.DisconnectAsync(true, cancellationToken);
            }
            catch (EmailDeliveryException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "No se pudo entregar el correo a {ToAddress}.", message.ToAddress);
                throw new EmailDeliveryException("No se pudo entregar el correo electrónico. Revisa la configuración SMTP.", exception);
            }
        }
    }
}
