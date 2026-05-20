using Domain.Database;

namespace Domain.Factories;

public interface IRepositoryFactory<TEntity, TId, TFields, TOptions>
    where TEntity : class
    where TFields : Enum
    where TOptions : IQueryOptions<TFields>
{
    ICrudRepository<TEntity, TId, TFields, TOptions> Create();
}
