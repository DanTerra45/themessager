using Mercadito.src.application.notifications.models;

namespace Mercadito.src.application.notifications.ports.output
{
    public interface IEmailSender
    {
        void EnsureAvailable();
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }
}
