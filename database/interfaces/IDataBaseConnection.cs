using System;
using System.Data;

namespace Mercadito;

public interface IDataBaseConnection : IDisposable
{
    IDbConnection CreateConnection();
    Task<IDbConnection> CreateConnectionAsync();
}
