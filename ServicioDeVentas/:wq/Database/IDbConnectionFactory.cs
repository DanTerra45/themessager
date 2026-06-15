using System.Data;
using System.Data.Common;

namespace Domain.Database
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
        Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken=default);
    }
}