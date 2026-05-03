using System.Data;
using MySqlConnector;

namespace Mercadito.src.shared.infrastructure.persistence;

public class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection no esta configurado.");
        }

        _connectionString = connectionString;
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = new MySqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch (MySqlException exception)
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
