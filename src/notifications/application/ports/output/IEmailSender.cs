using Mercadito.src.notifications.application.models;

namespace Mercadito.src.notifications.application.ports.output
{
    public interface IEmailSender
    {
        void EnsureAvailable();
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }
}
