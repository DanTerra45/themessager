using Domain.Common;

namespace Domain.Database
{
    public interface ICrudRepository<TEntity, TId, TFields, TOptions>
        where TEntity : class
        where TFields : Enum
        where TOptions : IQueryOptions<TFields>
    {
        Task<Result<IEnumerable<TEntity>>> GetAllAsync(TOptions? options);
        Task<Result<TEntity>> GetByIdAsync(TId id, TOptions? options);
        Task<Result<TEntity>> GetOneAsync(TOptions? options);
        Task<Result<TId>> CreateAsync<TRequest>(TRequest request, TOptions? options, CancellationToken cancellationToken = default) where TRequest : class;
        Task<Result<bool>> UpdateAsync<TRequest>(TRequest request, TOptions? options, CancellationToken cancellationToken = default) where TRequest : class;
        Task<Result<bool>> DeleteAsync(TOptions? options, CancellationToken cancellationToken = default);
    }
}