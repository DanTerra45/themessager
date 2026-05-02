using System.Data;

namespace Mercadito.Users.Api.Infrastructure.Shared.Persistence;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
