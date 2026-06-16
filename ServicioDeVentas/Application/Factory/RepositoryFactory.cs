using Application.Options;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;
using Domain.Factories;
using Infrastructure.Repository;

namespace Application.Factory
{
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public RepositoryFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public TRepository Create<TRepository>() where TRepository : class, IBaseRepository
        {
            return _serviceProvider.GetRequiredService<TRepository>();
        }
    }
}