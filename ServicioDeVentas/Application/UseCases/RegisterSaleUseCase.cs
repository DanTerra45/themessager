using System.Data;
using System.Numerics;
using Application.Options;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Dto.Register;
using Domain.Dto.Response;
using Domain.Entities;
using Domain.Factories;
using Domain.Mapper;
using Domain.Repository;

namespace Application.UseCases
{
  public class RegisterSaleUseCase
  {
    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISaleDetailRepository _saleDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    public RegisterSaleUseCase(IRepositoryFactory factory, IUnitOfWork unitOfWork)
    {
      _saleRepository = factory.Create<ISaleRepository>();
      _customerRepository = factory.Create<ICustomerRepository>();
      _saleDetailRepository = factory.Create<ISaleDetailRepository>();
      _unitOfWork = unitOfWork;
    }

    private async Task<Result<bool>> IsAValidCustomer(int CustomerId)
    {
      var result = await _customerRepository.GetByIdAsync(CustomerId, null);
      return result.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(new AppError("404", "The Customer is not Found", ErrorType.NotFound));
    }

    private async Task<Result<int>> RegisterSale(RegisterSale sale)
    {
      var options = new SaleOptions();
      options.SelectFields([SaleFields.CustomerId, SaleFields.OperatorId, SaleFields.State, SaleFields.CreatedAt]);
      var result = await _saleRepository.CreateAsync<RegisterSale>(sale, options);
      return result;
    }
    public async Task<Result<SaleResponseDto>> ExecuteAsync(RegisterSaleRequestDto request, int OperatorId)
    {
      var customerFound = await this.IsAValidCustomer(request.CustomerId);
      if (customerFound.IsFailure)
      {
        return Result<SaleResponseDto>.Failure(customerFound.Errors);
      }

      await using (_unitOfWork)
      {
        try
        {
          await _unitOfWork.BeginAsync();

          var saleResult = await this.RegisterSale(new RegisterSale(
              CustomerId: request.CustomerId,
              OperatorId: OperatorId,
              TotalPrice: request.TotalPrice
          ));
          if (saleResult.IsFailure)
          {
            await _unitOfWork.RollbackAsync();
            return Result<SaleResponseDto>.Failure(saleResult.Errors);
          }

          int saleId = saleResult.Value;
          var detailsToRegister = request.Details.Select(detail => new RegisterSaleDetail(
              SaleId: saleId,
              ProductId: detail.ProductId,
              Quantity: detail.Quantity,
              UnitPrice: detail.UnitPrice,
              SubTotal: detail.CalculateSubTotal
          )).ToList();

          var detailsResult = await this.RegisterSaleDetails(detailsToRegister, _unitOfWork.Transaction);
          if (detailsResult.IsFailure)
          {
            await _unitOfWork.RollbackAsync();
            return Result<SaleResponseDto>.Failure(detailsResult.Errors);
          }

          await _unitOfWork.CommitAsync();
          var customer = await this._customerRepository.GetByIdAsync(request.CustomerId, null);
          var response = await _saleRepository.GetByIdAsync(saleId, null);

          return Result<SaleResponseDto>.Success(response.Value.ToResponseDto(customer.Value.ToResponseDto()));
        }
        catch (Exception ex)
        {
          await _unitOfWork.RollbackAsync();
          return Result<SaleResponseDto>.Failure(
              new AppError("SaleExecutionError", ex.Message, ErrorType.Internal)
          );
        }
      }
    }

    private async Task<Result<bool>> RegisterSaleDetails(IEnumerable<RegisterSaleDetail> details, IDbTransaction transaction)
    {
      var options = new SaleDetailOptions();
      options.SelectFields([
          SaleDetailFields.SaleId,
        SaleDetailFields.ProductId,
        SaleDetailFields.Quantity,
        SaleDetailFields.UnitPrice,
        SaleDetailFields.SubTotal
      ]);

      var result = await _saleDetailRepository.CreateManyAsync<RegisterSaleDetail>(details, options, transaction);

      return result.IsSuccess
          ? Result<bool>.Success(true)
          : Result<bool>.Failure(new AppError("401", "No se pudieron registrar los detalles", ErrorType.Internal));
    }
  }
}
