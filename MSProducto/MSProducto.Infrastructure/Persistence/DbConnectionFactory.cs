namespace MSProducto.Infrastructure.Persistence
{
    using System.Data;
    using MySql.Data.MySqlClient;

    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }

    public class MySqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _cs;
        public MySqlConnectionFactory(string cs) => _cs = cs;
        public IDbConnection CreateConnection() => new MySqlConnection(_cs);
    }
}