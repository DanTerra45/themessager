using System.Data;

namespace ServicioEmpleados.Infrastructure.Shared.Persistence;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}

