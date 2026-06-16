using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ServicioReportes.Application.Common.Ports;

namespace ServicioReportes.Infrastructure.Shared.Persistence;

public sealed class NpgsqlConnectionFactory(IConfiguration configuration) : IReportsDbConnectionFactory
{
    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("PostgresConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Falta ConnectionStrings:PostgresConnection para ServicioReportes.");
        }

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
