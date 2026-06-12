using System.Numerics;
using Application.Options;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Dto.Register;
using Domain.Dto.Response;
using Domain.Factories;
using Domain.Repository;

namespace Application.UseCases{
  public class RegisterSaleUseCase{
    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;
    public RegisterSaleUseCase(IRepositoryFactory factory){
      _saleRepository = factory.Create<ISaleRepository>();
      _customerRepository = factory.Create<ICustomerRepository>();
    }
    
    private async Task<Result<bool>> IsAValidCustomer(int CustomerId){
      var result = await _customerRepository.GetByIdAsync(CustomerId,null);
      return result.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(new AppError("404","The Customer is not Found",ErrorType.NotFound)); 
    }
    
    private async Task<Result<int>> RegisterSale(RegisterSale sale){
      var options = new SaleOptions();
      options.SelectFields([SaleFields.CustomerId,SaleFields.OperatorId,SaleFields.State,SaleFields.CreatedAt]);
      var result = await _saleRepository.CreateAsync<RegisterSale>(sale,options);
      return result;
    }
    private async Task<Result<bool>> RegisterSaleDetails(IEnumerable<RegisterSaleDetail> Details){
      
    }

    public async Task<Result<SaleResponseDto>> ExecuteAsync(RegisterSaleRequestDto request,int OperatorId){
      var customerFound = await this.IsAValidCustomer(request.CustomerId);
      if(customerFound.IsFailure){
        return Result<SaleResponseDto>.Failure(customerFound.Errors);
      }
      var saleId = await this.RegisterSale(new RegisterSale(
            CustomerId:request.CustomerId,
            OperatorId:OperatorId,
            TotalPrice:request.TotalPrice
      ));
      if(saleId.IsFailure){
        return Result<SaleResponseDto>.Failure(saleId.Errors);
      }

    }
  }
}
