using System.Data;
using Domain.Database;
using Npgsql;

namespace Infrastructure.Database;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgresConnection")!;
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("La cadena de conexion no puede ser nula o vacia");
        }
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}