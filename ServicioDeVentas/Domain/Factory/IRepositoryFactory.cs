using Domain.Database;

namespace Domain.Factories;

public interface IRepositoryFactory
{
    public TRepository Create<TRepository>() where TRepository : class, IBaseRepository;
}
