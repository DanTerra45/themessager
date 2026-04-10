using System.Data;

namespace Mercadito.src.shared.infrastructure.persistence;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
