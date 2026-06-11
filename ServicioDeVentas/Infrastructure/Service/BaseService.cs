using Domain.Database;
using Domain.Service;

namespace Infrastructure.Service
{
    public abstract class BaseService<TRepository, TEntity, TId, TFields, TOptions> : IService<TEntity, TId, TFields, TOptions>
        where TRepository : class, ICrudRepository<TEntity, TId, TFields, TOptions>
        where TEntity : class
        where TFields : Enum
        where TOptions : IQueryOptions<TFields>
    {
        protected readonly TRepository _repository;
        protected BaseService(TRepository repository)
        {
            _repository = repository;
        }
    }
}