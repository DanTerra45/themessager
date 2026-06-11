using Application.Options;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;
using Domain.Factories;
using Infrastructure.Repository;

namespace Application.Factory
{
    public class SaleFactory : IRepositoryFactory<SaleWithDetails,int,SaleFields,SaleOptions>
    {
        private readonly SaleRepository _repository;
        public SaleFactory(SaleRepository repository)
        {
            _repository = repository;
        }
        public ICrudRepository<SaleWithDetails, int, SaleFields, SaleOptions> Create()
        {
            return _repository;
        }
    }
}