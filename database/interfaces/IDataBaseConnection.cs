using System.Data;

namespace Mercadito.database.interfaces;

public interface IDataBaseConnection
{
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
