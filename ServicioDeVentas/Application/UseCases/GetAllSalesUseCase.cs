using Application.Options;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Dto.Response;
using Domain.Entities;
using Domain.Mapper;

namespace Application.UseCases
{
    public class GetAllSalesUseCase
    {
        private readonly ICrudRepository<SaleWithDetails,int,SaleFields,SaleOptions> _saleRepository;
        private readonly ICrudRepository<Customer,int,CustomerFields,CustomerOptions> _customerRepository;

        public GetAllSalesUseCase(ICrudRepository<SaleWithDetails,int,SaleFields,SaleOptions> saleRepository, ICrudRepository<Customer,int,CustomerFields,CustomerOptions> customerRepository)
        {
            this._saleRepository = saleRepository;
            this._customerRepository = customerRepository;
        }
        public async Task<Result<IReadOnlyList<SaleResponseDto>>> ExecuteAsync(SaleOptions? options)
        {
            var sales = await _saleRepository.GetAllAsync(options);
            if (!sales.IsSuccess || sales.Value is null)
            {
                return Result<IReadOnlyList<SaleResponseDto>>.Failure(sales.Errors);
            }

            var customers = await _customerRepository.GetAllAsync(null);
            if (!customers.IsSuccess || customers.Value is null)
            {
                return Result<IReadOnlyList<SaleResponseDto>>.Failure(customers.Errors);
            }

            var customerDicctionary = customers.Value.ToDictionary(c => c.Id, c => c);
            var response = new List<SaleResponseDto>();

            foreach (var sale in sales.Value)
            {
                if (!customerDicctionary.TryGetValue(sale.CustomerId, out var customer))
                {
                    return Result<IReadOnlyList<SaleResponseDto>>.Failure(
                        new AppError(
                            "CustomerNotFound",
                            $"No se encontro el cliente con id {sale.CustomerId} para la venta {sale.Id}.",
                            ErrorType.NotFound
                        )
                    );
                }

                response.Add(sale.ToResponseDto(customer.ToResponseDto()));
            }

            return Result<IReadOnlyList<SaleResponseDto>>.Success(response);
        }
    }
}