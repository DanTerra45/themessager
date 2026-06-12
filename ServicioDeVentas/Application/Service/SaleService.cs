using Application.Options;
using Domain.Common;
using Domain.Database.Fields;
using Domain.Dto.Response;
using Domain.Entities;
using Domain.Factories;
using Domain.Repository;
using Domain.Service;
using Infrastructure.Repository;

namespace Application.Service{
  public class SaleService
  {
    private readonly ISaleRepository _repository;
    public SaleService(IRepositoryFactory factory)
    {
      _repository = factory.Create<ISaleRepository>();
    }
    public async Task<Result<SaleWithDetails>> GetOne(SaleOptions options)
    {
      return await _repository.GetOneAsync(options);
    }
  }
}
