using Application.Options;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Dto.Response;
using Domain.Entities;
using Domain.Factories;
using Domain.Mapper;
using Domain.Repository;
using Infrastructure.Repository;

namespace Application.UseCases{
  public class GetSaleByUseCase{
    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;
    public GetSaleByUseCase(IRepositoryFactory factory){
      _saleRepository = factory.Create<ISaleRepository>();
      _customerRepository = factory.Create<ICustomerRepository>();
    }
    public async Task<Result<SaleResponseDto>> ExecuteAsync(SaleOptions options){
      var saleResult = await _saleRepository.GetOneAsync(options);
      if(!saleResult.IsSuccess || saleResult.Value is null){
        return Result<SaleResponseDto>.Failure(saleResult.Errors);
      }
      var sale = saleResult.Value;
      var customerOptions = new CustomerOptions();
      customerOptions.AddFilter(CustomerFields.Id, FilterOperator.Equals, sale.CustomerId);
      var customerResult = await _customerRepository.GetOneAsync(customerOptions);
      if(!customerResult.IsSuccess || customerResult.Value is null){
        return Result<SaleResponseDto>.Failure(customerResult.Errors);
      }
      var customer = customerResult.Value;

      return Result<SaleResponseDto>.Success(sale.ToResponseDto(customer.ToResponseDto()));
    }
  }
}
