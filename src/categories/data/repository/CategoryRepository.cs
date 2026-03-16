using Dapper;
using Mercadito.database.interfaces;
using Mercadito.src.categories.data.entity;
using Mercadito.src.categories.domain.model;
using Mercadito.src.categories.domain.repository;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.categories.data.repository
{
    public class CategoryRepository(IDataBaseConnection dbConnection) : ICategoryRepository
    {
        private const string ActiveState = "A";
        private const string InactiveState = "I";

        private readonly IDataBaseConnection _dbConnection = dbConnection;

        public async Task<IReadOnlyList<CategoryModel>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"SELECT id AS Id, 
                        codigo AS Code, 
                        nombre AS Name, 
                        descripcion AS Description, 
                        0 AS ProductCount 
                        FROM categorias 
                        WHERE estado = @ActiveState";

            var command = new CommandDefinition(query, parameters: new { ActiveState }, cancellationToken: cancellationToken);
            var categories = await connection.QueryAsync<CategoryModel>(command);
            return categories.AsList();
        }

        public async Task<int> GetTotalCategoriesCountAsync(CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = "SELECT COUNT(*) FROM categorias WHERE estado = @ActiveState";

            var command = new CommandDefinition(query, parameters: new { ActiveState }, cancellationToken: cancellationToken);
            return await connection.ExecuteScalarAsync<int>(command);
        }

        public async Task<IReadOnlyList<CategoryModel>> GetCategoryByPages(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var offset = (page - 1) * pageSize;
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"SELECT c.id AS Id, 
                        c.codigo AS Code, 
                        c.nombre AS Name, 
                        c.descripcion AS Description, 
                        COUNT(DISTINCT p.id) AS ProductCount 
                        FROM categorias c 
                        LEFT JOIN categoriaDeProducto cp ON c.id = cp.categoriaId 
                        LEFT JOIN products p ON cp.productId = p.id AND p.estado = @ActiveState
                        WHERE c.estado = @ActiveState
                        GROUP BY c.id, c.codigo, c.nombre, c.descripcion 
                        ORDER BY c.id 
                        LIMIT @PageSize OFFSET @Offset";

            var command = new CommandDefinition(
                query,
                parameters: new { ActiveState, Offset = offset, PageSize = pageSize },
                cancellationToken: cancellationToken);

            var categories = await connection.QueryAsync<CategoryModel>(command);
            return categories.AsList();
        }

        public async Task<CategoryModel?> GetCategoryByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"SELECT c.id AS Id, 
                        c.codigo AS Code, 
                        c.nombre AS Name, 
                        c.descripcion AS Description, 
                        COUNT(DISTINCT p.id) AS ProductCount 
                        FROM categorias c 
                        LEFT JOIN categoriaDeProducto cp ON c.id = cp.categoriaId 
                        LEFT JOIN products p ON cp.productId = p.id AND p.estado = @ActiveState
                        WHERE c.id = @Id AND c.estado = @ActiveState
                        GROUP BY c.id, c.codigo, c.nombre, c.descripcion";

            var command = new CommandDefinition(
                query,
                parameters: new { Id = id, ActiveState },
                cancellationToken: cancellationToken);

            return await connection.QueryFirstOrDefaultAsync<CategoryModel>(command);
        }

        public async Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                const string query = @"INSERT INTO categorias 
                        (codigo, nombre, descripcion, estado) VALUES (@Code, @Name, @Description, @ActiveState)";
                var command = new CommandDefinition(
                    query,
                    parameters: new { category.Code, category.Name, category.Description, ActiveState },
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(command);
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                throw new ValidationException("Ya existe una categoria con ese codigo.");
            }
        }

        public async Task<int> UpdateCategoryAsync(Category category, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"UPDATE categorias
         SET codigo = @Code, nombre = @Name, descripcion = @Description
         WHERE id = @Id AND estado = @ActiveState";

            var command = new CommandDefinition(
                query,
                parameters: new { category.Id, category.Code, category.Name, category.Description, ActiveState },
                cancellationToken: cancellationToken);

            return await connection.ExecuteAsync(command);
        }

        public async Task<int> DeleteCategoryAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            const string query = @"UPDATE categorias
         SET estado = @InactiveState
         WHERE id = @Id AND estado = @ActiveState";

            var command = new CommandDefinition(
                query,
                parameters: new { id, ActiveState, InactiveState },
                cancellationToken: cancellationToken);

            return await connection.ExecuteAsync(command);

        }
    }
}

