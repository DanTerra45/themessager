using Mercadito.src.categories.data.entity;
using Mercadito.src.categories.domain.model;
using Mercadito.src.categories.domain.repository;
using Mercadito.database.interfaces;
using Dapper;

namespace Mercadito.src.categories.data.repository
{
    public class CategoryRepository(IDataBaseConnection dbConnection) : ICategoryRepository
    {
        private readonly IDataBaseConnection _dbConnection = dbConnection;

        public async Task<IEnumerable<CategoryModel>> GetAllCategoriesAsync()
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            const string query = @"SELECT id AS Id, 
                        codigo AS Code, 
                        nombre AS Name, 
                        descripcion AS Description, 
                        0 AS ProductCount 
                        FROM categorias 
                        WHERE estado = 'A'";
            return await connection.QueryAsync<CategoryModel>(query);
        }

        public async Task<int> GetTotalCategoriesCountAsync()
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            const string query = "SELECT COUNT(*) FROM categorias WHERE estado = 'A'";
            return await connection.ExecuteScalarAsync<int>(query);
        }

        public async Task<IEnumerable<CategoryModel>> GetCategoryByPages(int page, int pageSize)
        {
            int offset = (page - 1) * pageSize;
            using var connection = await _dbConnection.CreateConnectionAsync();
            const string query = @"SELECT c.id AS Id, 
                        c.codigo AS Code, 
                        c.nombre AS Name, 
                        c.descripcion AS Description, 
                        COUNT(DISTINCT p.id) AS ProductCount 
                        FROM categorias c 
                        LEFT JOIN categoriaDeProducto cp ON c.id = cp.categoriaId 
                        LEFT JOIN products p ON cp.productId = p.id AND p.estado = 'A'
                        WHERE c.estado = 'A'
                        GROUP BY c.id, c.codigo, c.nombre, c.descripcion 
                        ORDER BY c.id 
                        LIMIT @PageSize OFFSET @Offset";
            return await connection.QueryAsync<CategoryModel>(query, new { Offset = offset, PageSize = pageSize });
        }

        public async Task<CategoryModel?> GetCategoryByIdAsync(long id)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            const string query = @"SELECT c.id AS Id, 
                        c.codigo AS Code, 
                        c.nombre AS Name, 
                        c.descripcion AS Description, 
                        COUNT(DISTINCT p.id) AS ProductCount 
                        FROM categorias c 
                        LEFT JOIN categoriaDeProducto cp ON c.id = cp.categoriaId 
                        LEFT JOIN products p ON cp.productId = p.id AND p.estado = 'A'
                        WHERE c.id = @Id AND c.estado = 'A'
                        GROUP BY c.id, c.codigo, c.nombre, c.descripcion";
            return await connection.QueryFirstOrDefaultAsync<CategoryModel>(query, new { Id = id });
        }

        public async Task AddCategoryAsync(Category category)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            const string query = @"INSERT INTO categorias 
                        (codigo, nombre, descripcion, estado) VALUES (@Code, @Name, @Description, 'A')";
            await connection.ExecuteAsync(query, new { category.Code, category.Name, category.Description });
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            const string query = @"UPDATE categorias 
                        SET codigo = @Code, nombre = @Name, descripcion = @Description 
                        WHERE id = @Id";
            await connection.ExecuteAsync(query, new { category.Id, category.Code, category.Name, category.Description });
        }

        public async Task<int> DeleteCategoryAsync(long id)
        {
            using var connection = await _dbConnection.CreateConnectionAsync();
            const string query = @"UPDATE categorias 
                        SET estado = 'I' 
                        WHERE id = @Id AND estado = 'A'";
            return await connection.ExecuteAsync(query, new { id });
        }
    }
}
