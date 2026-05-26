using System.Data;

namespace Mercadito.Sales.Api.Infrastructure.Shared.Persistence;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
