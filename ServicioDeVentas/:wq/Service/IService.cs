using Domain.Database;

namespace Domain.Service{
  public interface IService<TEntity,TId, TFields, TOptions>
  where TEntity : class
  where TFields : Enum
  where TOptions : IQueryOptions<TFields>
  {
    
  }
}
