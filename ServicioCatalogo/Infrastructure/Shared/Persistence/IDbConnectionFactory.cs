using System.Data;

namespace ServicioCatalogo.Infrastructure.Shared.Persistence;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}

