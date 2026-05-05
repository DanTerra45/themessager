using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Domain.Users.Entities;

namespace Mercadito.Users.Api.Application.Users.Ports.Output
{
    public interface IUserRepository
    {
        Task<User?> GetActiveByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<User?> GetActiveByIdAsync(long userId, CancellationToken cancellationToken = default);
        Task<User?> GetActiveByUsernameOrEmailAsync(string identifier, CancellationToken cancellationToken = default);
        Task<string> GenerateUniqueUsernameAsync(string seed, CancellationToken cancellationToken = default);
        Task UpdateLastLoginAsync(long userId, DateTime lastLogin, CancellationToken cancellationToken = default);
        Task<long> CreateAsync(CreateUserDto user, string passwordHash, CancellationToken cancellationToken = default);
        Task<bool> DeactivateAsync(long userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserListItem>> GetAllActiveAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AvailableEmployeeOption>> GetAvailableEmployeesAsync(CancellationToken cancellationToken = default);
    }
}
