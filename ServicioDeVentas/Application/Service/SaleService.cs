using Application.Options;
using Domain.Common;
using Domain.Database.Fields;
using Domain.Dto.Response;
using Domain.Entities;
using Domain.Factories;
using Domain.Service;
using Infrastructure.Repository;
using Infrastructure.Service;

namespace Application.Service{
  public class SaleService : BaseService<SaleRepository, SaleWithDetails, int, SaleFields, SaleOptions>
  {
    public SaleService(IRepositoryFactory<SaleWithDetails, int, SaleFields, SaleOptions> repositoryFactory) : base((SaleRepository)repositoryFactory.Create())
    {
    }
    public async Task<Result<SaleWithDetails>> GetOne(SaleOptions options)
    {
      return await _repository.GetOneAsync(options);
    }
  }
}
