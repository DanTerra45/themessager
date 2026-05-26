using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ServicioEmpleados.Infrastructure.Shared.Persistence;

public class PostgresConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public PostgresConnectionFactory(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection no está configurado.");
        }

        _connectionString = connectionString;
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = new NpgsqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch (PostgresException exception)
        {
            connection.Dispose();
            throw new InvalidOperationException(
                "No se pudo abrir una conexión con la base de datos. Inténtelo nuevamente más tarde.",
                exception);
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }
}
