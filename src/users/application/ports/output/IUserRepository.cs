using Mercadito.src.users.application.models;
using Mercadito.src.users.domain.entities;
using Mercadito.src.notifications.application.models;

namespace Mercadito.src.users.application.ports.output
{
    public interface IUserRepository
    {
        Task<User?> GetActiveByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<User?> GetActiveByIdAsync(long userId, CancellationToken cancellationToken = default);
        Task<User?> GetActiveByUsernameOrEmailAsync(string identifier, CancellationToken cancellationToken = default);
        Task<PasswordResetTokenInfo?> GetValidPasswordResetTokenAsync(string tokenHash, DateTime currentUtc, CancellationToken cancellationToken = default);
        Task<string> GenerateUniqueUsernameAsync(string seed, CancellationToken cancellationToken = default);
        Task UpdateLastLoginAsync(long userId, DateTime lastLogin, CancellationToken cancellationToken = default);
        Task<long> CreateAsync(CreateUserDto user, string passwordHash, CancellationToken cancellationToken = default);
        Task<long> CreateWithOnboardingAsync(CreateUserDto user, string passwordHash, string tokenHash, DateTime expiresAtUtc, EmailMessage onboardingEmail, CancellationToken cancellationToken = default);
        Task<bool> ResetPasswordAsync(long userId, string passwordHash, CancellationToken cancellationToken = default);
        Task<bool> ResetPasswordAndQueueNotificationAsync(long userId, string passwordHash, EmailMessage? notificationEmail, CancellationToken cancellationToken = default);
        Task<bool> DeactivateAsync(long userId, CancellationToken cancellationToken = default);
        Task CreatePasswordResetTokenAsync(long userId, string tokenHash, DateTime expiresAtUtc, DateTime invalidatedAtUtc, CancellationToken cancellationToken = default);
        Task CreatePasswordResetTokenAndQueueEmailAsync(long userId, string tokenHash, DateTime expiresAtUtc, DateTime invalidatedAtUtc, EmailMessage emailMessage, CancellationToken cancellationToken = default);
        Task<bool> ResetPasswordByTokenAsync(string tokenHash, string passwordHash, DateTime currentUtc, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserListItem>> GetAllActiveAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AvailableEmployeeOption>> GetAvailableEmployeesAsync(CancellationToken cancellationToken = default);
    }
}
