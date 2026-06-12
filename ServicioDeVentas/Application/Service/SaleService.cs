using Application.Options;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Dto.Response;
using Domain.Dto.Update;
using Domain.Entities;
using Domain.Entities.Enums;
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
    public async Task<Result<bool>> MarkSaleAsComplete(int saleId){
      return await this.UpdateSaleState(saleId,SaleState.Confirmed);
    }
    public async Task<Result<bool>> MarkSaleAsCancel(int saleId){
      return await this.UpdateSaleState(saleId,SaleState.Cancelled);
    }
    private async Task<Result<bool>> UpdateSaleState(int saleId, SaleState state){
      var options = new SaleOptions();
      options.AddFilter(SaleFields.Id,FilterOperator.Equals,saleId);
      var result = await _repository.UpdateAsync<UpdateSaleState>(new UpdateSaleState(saleId,state),options);
      return result;
    }
  }
}
