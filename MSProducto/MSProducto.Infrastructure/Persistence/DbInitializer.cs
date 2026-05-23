namespace MSProducto.Infrastructure.Persistence
{
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper;
    using MySql.Data.MySqlClient;

    public class DbInitializer
    {
        private readonly IDbConnectionFactory _factory;

        public DbInitializer(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            using var connection = (MySqlConnection)_factory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            const string createCategoriasTable = @"
CREATE TABLE IF NOT EXISTS categorias (
    id                    INT AUTO_INCREMENT PRIMARY KEY,
    codigo                VARCHAR(50) NOT NULL UNIQUE,
    nombre                VARCHAR(100) NOT NULL,
    descripcion           VARCHAR(255),
    productosActivosCount INT NOT NULL DEFAULT 0,
    estado                TINYINT(1) NOT NULL DEFAULT 1,
    fechaRegistro         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ultimaActualizacion   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);";

            const string createProductosTable = @"
CREATE TABLE IF NOT EXISTS productos (
    id                  INT AUTO_INCREMENT PRIMARY KEY,
    nombre              VARCHAR(150) NOT NULL,
    descripcion         VARCHAR(500),
    lote                VARCHAR(100),
    fechaCaducidad      DATE,
    precio              DECIMAL(10,2) NOT NULL,
    stock               INT NOT NULL DEFAULT 0,
    estado              TINYINT(1) NOT NULL DEFAULT 1,
    activoUnico         TINYINT(1) NOT NULL DEFAULT 0,
    fechaRegistro       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ultimaActualizacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);";

            const string createCategoriaDeProductoTable = @"
CREATE TABLE IF NOT EXISTS categoriaDeProducto (
    productId  INT NOT NULL,
    categoriaId INT NOT NULL,
    PRIMARY KEY (productId, categoriaId),
    FOREIGN KEY (productId) REFERENCES productos(id) ON DELETE CASCADE,
    FOREIGN KEY (categoriaId) REFERENCES categorias(id) ON DELETE CASCADE
);";

            await connection.ExecuteAsync(createCategoriasTable);
            await connection.ExecuteAsync(createProductosTable);
            await connection.ExecuteAsync(createCategoriaDeProductoTable);
        }
    }
}