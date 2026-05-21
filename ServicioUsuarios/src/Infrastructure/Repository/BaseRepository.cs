using System.Data;
using Dapper;
using Domain.Common;
using Domain.Database;

namespace Infrastructure.Repository
{
    public abstract class BaseRepository<TEntity,TId,TFields,TOptions,TSchema> : ICrudRepository<TEntity, TId,TFields, TOptions>
    where TEntity : class
    where TFields : Enum
    where TOptions : class, IQueryOptions<TFields>, new()
    where TSchema : class, ITableSchema<TFields>, new()
    {
        protected readonly IDbConnectionFactory _db;
        private readonly ILogger<BaseRepository<TEntity,TId,TFields,TOptions,TSchema>> _logger;
        protected string _tableName = string.Empty;
        public BaseRepository(IDbConnectionFactory db,string tableName, ILogger<BaseRepository<TEntity,TId,TFields,TOptions,TSchema>> logger)
        {
            _db = db;
            _tableName = tableName; 
            _logger = logger;
        }
        private async Task<Result<IEnumerable<TEntity>>> ExecuteQueryAsync(string sql, DynamicParameters parameters)
        {
            using var connection =await _db.CreateConnectionAsync();
            var result = await connection.QueryAsync<TEntity>(sql,parameters);
            return Result<IEnumerable<TEntity>>.Success(result);
        }
        private async Task<int> ExecuteNonQueryAsync(string sql, DynamicParameters parameters)
        {
            using var connection = await _db.CreateConnectionAsync();
            return await connection.ExecuteAsync(sql,parameters);
        }
        public async Task<Result<IEnumerable<TEntity>>> GetAllAsync(TOptions? options)
        {
            var (sql, parameters) = new QueryBuilder<TOptions,TFields>(_tableName, new TSchema())
                    .Select(options ?? new TOptions())
                    .Where(options ?? new TOptions())
                    .Build();
                _logger.LogInformation("Executing SQL: {Sql} with parameters: {@Parameters}", sql, parameters);
            try
            {
                var result = await ExecuteQueryAsync(sql, parameters);
                return result;
            }catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing SQL: {Sql} with parameters: {@Parameters}", sql, parameters);
                return Task.FromResult(Result<IEnumerable<TEntity>>.Failure(new AppError(
                    Code: "500",
                    Message: ex.Message,
                    Type: ErrorType.Internal
                )));
            }
        }
        public Task<Result<TEntity>> GetByIdAsync(TId id, TOptions? options)
        {
            throw new NotImplementedException();
        }
        public Task<Result<TId>> CreateAsync<TRequest>(TRequest entity, TOptions? options, CancellationToken cancellationToken = default) where TRequest : class
        {
            throw new NotImplementedException();
        }
        public Task<Result<bool>> UpdateAsync<TRequest>(TRequest entity, TOptions? options, CancellationToken cancellationToken = default) where TRequest : class
        {
            throw new NotImplementedException();
        }
        public Task<Result<bool>> DeleteAsync(TId id, TOptions? options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}