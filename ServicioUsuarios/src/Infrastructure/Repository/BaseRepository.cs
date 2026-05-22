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
                return Result<IEnumerable<TEntity>>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
            }
        }
        public Task<Result<TEntity>> GetByIdAsync(TId id, TOptions? options)
        {
            throw new NotImplementedException();
        }
        public async Task<Result<TEntity>> GetOneAsync(TOptions? options)
        {
            var (sql, parameters) = new QueryBuilder<TOptions,TFields>(_tableName, new TSchema())
                    .Select(options ?? new TOptions())
                    .Where(options ?? new TOptions())
                    .Build();
                _logger.LogInformation("Executing SQL: {Sql} with parameters: {@Parameters}", sql, parameters);
            try{
                var result = await ExecuteQueryAsync(sql, parameters);
                var entity = result.Value?.FirstOrDefault();
                if (entity == null)
                {
                    _logger.LogWarning("No entity found for SQL: {Sql} with parameters: {@Parameters}", sql, parameters);
                    return Result<TEntity>.Failure(new AppError("NotFound", "No entity found matching the criteria.", ErrorType.NotFound));
                }
                return Result<TEntity>.Success(entity);
            }catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing SQL: {Sql} with parameters: {@Parameters}", sql, parameters);
                return Result<TEntity>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
            }
        }
        public async Task<Result<TId>> CreateAsync<TRequest>(TRequest entity, TOptions? options, CancellationToken cancellationToken = default) where TRequest : class
        {
            try
            {
                var (sql, parameters) = new QueryBuilder<TOptions, TFields>(_tableName, new TSchema())
                    .Insert<TRequest>(options ?? new TOptions(), entity)
                    .Build();
                _logger.LogInformation("Executing SQL: {Sql}", sql);
                foreach(var p in parameters.ParameterNames)
                {
                    _logger.LogInformation("Parameter: {Name} = {@Value}", p, parameters.Get<object?>(p));
                }
                

                var result = await ExecuteNonQueryAsync(sql, parameters);
                if (result > 0)
                {
                    return Result<TId>.Success(default!);
                }
                else
                {
                    _logger.LogWarning("No rows affected when trying to create entity: {Entity} with options: {@Options}", entity, options);
                    return Result<TId>.Failure(new AppError("InsertFailed", "Failed to insert the entity.", ErrorType.Internal));
                }
                // throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating entity: {Entity} with options: {@Options}", entity, options);
                return Result<TId>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
            }
        }
        public async Task<Result<bool>> UpdateAsync<TRequest>(TRequest entity, TOptions? options, CancellationToken cancellationToken = default) where TRequest : class
        {
            var (sql, parameters) = new QueryBuilder<TOptions, TFields>(_tableName, new TSchema())
                .Update<TRequest>(options ?? new TOptions(), entity)
                .Where(options ?? new TOptions())
                .Build();
            _logger.LogInformation("Executing Update SQL: {Sql} with parameters: {@Parameters}", sql, parameters);
            try{
                var result = await ExecuteNonQueryAsync(sql, parameters);
                return Result<bool>.Success(result > 0);
            }catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing SQL: {Sql} with parameters: {@Parameters}", sql, parameters);
                return Result<bool>.Failure(new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal));
            }
        }
        public async Task<Result<bool>> DeleteAsync(TId id, TOptions? options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}