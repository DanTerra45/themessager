using Mercadito.Users.Api.Application.Notifications.Models;

namespace Mercadito.Users.Api.Application.Notifications.Ports.Output
{
    public interface IEmailSender
    {
        void EnsureAvailable();
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }
}
