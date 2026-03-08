using System.Data;
using Mercadito.database.interfaces;
using MySql.Data.MySqlClient;

namespace Mercadito.database;

public class MySqlConnectionFactory : IDataBaseConnection
{
    private readonly string _connectionString;
    private readonly ILogger<MySqlConnectionFactory> _logger;

    public MySqlConnectionFactory(IConfiguration configuration, ILogger<MySqlConnectionFactory> logger)
    {
        _logger = logger;

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection no esta configurado.");
        }

        _connectionString = connectionString;
    }

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        try
        {
            var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
        catch (MySqlException exception)
        {
            _logger.LogError(exception, "Error al crear conexion MySQL asincrona");
            throw;
        }
    }
}
