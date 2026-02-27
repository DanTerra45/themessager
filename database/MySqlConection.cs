using System.Data;
using MySql.Data.MySqlClient;

namespace Mercadito;

public class MySqlConnectionFactory : IDataBaseConnection
{
    private readonly string _connectionString;
    private readonly ILogger<MySqlConnectionFactory> _logger;

    public MySqlConnectionFactory(IConfiguration configuration, ILogger<MySqlConnectionFactory> logger)
    {
        _logger = logger;
        
        var server = configuration["Database:Server"] ?? "127.0.0.1";
        var port = configuration["Database:Port"] ?? "3306";
        var database = configuration["Database:Name"] ?? "mydb";
        var user = configuration["Database:User"] ?? "user";
        var password = configuration["Database:Password"] ?? "password";
        
        _connectionString = $"Server={server};Port={port};Database={database};Uid={user};Pwd={password};";
    }

    public IDbConnection CreateConnection()
    {
        try
        {
            var connection = new MySqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error al crear conexión MySQL");
            throw;
        }
    }

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        try
        {
            var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error al crear conexión MySQL asíncrona");
            throw;
        }
    }

    public void Dispose()
    {
        // Limpieza si es necesaria
    }
}
