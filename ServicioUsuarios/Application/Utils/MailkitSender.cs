using Application.Service;
using Domain.Dto.Email;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
namespace Application.Utils
{
    public sealed class MailKitEmailSender
    {
        private readonly SmtpOptions _options;

        public MailKitEmailSender(IOptions<SmtpOptions> options)
        {
            _options = options.Value;
        }

        public async Task SendAsync(MailMessage message, CancellationToken cancellationToken = default)
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            mimeMessage.To.Add(new MailboxAddress(message.ToName ?? message.ToAddress, message.ToAddress));
            mimeMessage.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder
            {
                TextBody = message.PlainTextBody,
                HtmlBody = message.HtmlBody
            };

            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port, _options.UseSsl, cancellationToken);
            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}