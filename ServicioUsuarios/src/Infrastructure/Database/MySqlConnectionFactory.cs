using System.Data;
using MySqlConnector;
using Domain.Database;
namespace Infrastructure.Database
{
    public class MySqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionStrig;
        public MySqlConnectionFactory(IConfiguration configuration)
        {
            _connectionStrig = configuration.GetConnectionString("MySqlConnection")!;
            if (string.IsNullOrEmpty(_connectionStrig))
            {
                throw new InvalidOperationException("La cadena de conexion no puede ser nula o vacia");
            }
        }
        public IDbConnection CreateConnection()
        {
            return new MySqlConnection(_connectionStrig);
        }
        public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new MySqlConnection(_connectionStrig);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}