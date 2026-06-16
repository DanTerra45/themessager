using System.Data;

namespace ServicioReportes.Application.Common.Ports;

public interface IReportsDbConnectionFactory
{
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
