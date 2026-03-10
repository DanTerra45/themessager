using System.Data;
using Mercadito.database.interfaces;
using MySql.Data.MySqlClient;

namespace Mercadito.database;

public class MySqlConnectionFactory : IDataBaseConnection
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
        try
        {
            var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch (MySqlException exception)
        {
            throw new InvalidOperationException(
                "No se pudo abrir una conexion a MySQL. Verifique la configuracion de acceso y la disponibilidad del servidor.",
                exception);
        }
    }
}
