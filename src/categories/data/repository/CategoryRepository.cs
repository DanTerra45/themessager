using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace Mercadito
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly IDataBaseConnection _dbConnection;
        private readonly string tableName = "categorias";
        private readonly string relationTableName = "categoriaDeProducto";
        private readonly ILogger<CategoryRepository> _logger;
        public CategoryRepository(IDataBaseConnection dbConnection, ILogger<CategoryRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }
        public async Task<IEnumerable<CategoryModel>> GetAllCategoriesAsync()
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"SELECT id AS Id, codigo AS Code, nombre AS Name, descripcion AS Description, 0 AS ProductCount FROM {tableName}";
                return await connection.QueryAsync<CategoryModel>(query);
                
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las categorías");
                throw;
            }
        }
        public async Task<IEnumerable<CategoryModel>> GetCategoryByPages(int page)
        {
            try
            {
                int pageSize = 10;
                int offset = (page - 1) * pageSize;
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"SELECT c.id AS Id, c.codigo AS Code, c.nombre AS Name, c.descripcion AS Description, COUNT(p.categoriaId) AS ProductCount FROM {tableName} c LEFT JOIN {relationTableName} p ON c.id = p.categoriaId GROUP BY c.id, c.codigo, c.nombre, c.descripcion ORDER BY c.id OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                return await connection.QueryAsync<CategoryModel>(query, new { Offset = offset, PageSize = pageSize });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener categorías por página: {page}");
                throw;
            }
    }
        public async Task<CategoryModel?> GetCategoryByIdAsync(Guid id)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"SELECT c.id AS Id, c.codigo AS Code, c.nombre AS Name, c.descripcion AS Description, COUNT(p.categoriaId) AS ProductCount FROM {tableName} c LEFT JOIN {relationTableName} p ON c.id = p.categoriaId WHERE c.id = @Id GROUP BY c.id, c.codigo, c.nombre, c.descripcion";
                return await connection.QueryFirstOrDefaultAsync<CategoryModel>(query, new { Id = id });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener la categoría con ID: {id}");
                throw;
            }
        }
        public async Task AddCategoryAsync(CreateCategoryDto category)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"INSERT INTO {tableName} (codigo, nombre, descripcion) VALUES (@Code, @Name, @Description)";
                await connection.ExecuteAsync(query, new { Code = category.Code, Name = category.Name, Description = category.Description });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al agregar la categoría");
                throw;
            }
        }
        public async Task UpdateCategoryAsync(Category category)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"UPDATE {tableName} SET codigo = @Code, nombre = @Name, descripcion = @Description WHERE id = @Id";
                await connection.ExecuteAsync(query, new { Id = category.Id, Code = category.Code, Name = category.Name, Description = category.Description });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la categoría");
                throw;
            }
        }
        public async Task DeleteCategoryAsync(Guid id)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync();
                var query = $"DELETE FROM {tableName} WHERE id = @Id";
                await connection.ExecuteAsync(query, new { Id = id });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la categoría");
                throw;
            }
        }
    }
}