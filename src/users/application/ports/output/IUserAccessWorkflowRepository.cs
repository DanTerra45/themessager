using Mercadito.src.notifications.application.models;
using Mercadito.src.users.application.models;

namespace Mercadito.src.users.application.ports.output
{
    public interface IUserAccessWorkflowRepository
    {
        Task<PasswordResetTokenInfo?> GetValidPasswordResetTokenAsync(string tokenHash, DateTime currentUtc, CancellationToken cancellationToken = default);
        Task<long> CreateWithOnboardingAsync(CreateUserDto user, string passwordHash, string tokenHash, DateTime expiresAtUtc, EmailMessage onboardingEmail, CancellationToken cancellationToken = default);
        Task<bool> BeginAdministrativePasswordResetAsync(long userId, string passwordHash, string tokenHash, DateTime expiresAtUtc, DateTime invalidatedAtUtc, EmailMessage emailMessage, CancellationToken cancellationToken = default);
        Task<bool> SetTemporaryPasswordAsync(long userId, string passwordHash, DateTime invalidatedAtUtc, CancellationToken cancellationToken = default);
        Task CreatePasswordResetTokenAndQueueEmailAsync(long userId, string tokenHash, DateTime expiresAtUtc, DateTime invalidatedAtUtc, EmailMessage emailMessage, CancellationToken cancellationToken = default);
        Task<bool> ResetPasswordByTokenAsync(string tokenHash, string passwordHash, DateTime currentUtc, CancellationToken cancellationToken = default);
        Task<bool> CompleteForcedPasswordChangeAsync(long userId, string passwordHash, DateTime invalidatedAtUtc, CancellationToken cancellationToken = default);
    }
}
