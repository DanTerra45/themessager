using Mercadito.Sales.Api.Application.Suppliers.Ports.Input;
using Mercadito.Sales.Api.Application.Suppliers.Ports.Output;
using Mercadito.Sales.Api.Domain.Shared;
using Mercadito.Sales.Api.Domain.Shared.Exceptions;

namespace Mercadito.Sales.Api.Application.Suppliers.UseCases
{
    public sealed class GetNextSupplierCodeUseCase(ISupplierRepository repository) : IGetNextSupplierCodeUseCase
    {
        public async Task<Result<string>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var nextCode = await repository.GetNextSupplierCodeAsync(cancellationToken);
                return Result.Success(nextCode);
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure<string>(validationException.Errors);
                }

                return Result.Failure<string>(new Dictionary<string, List<string>>
                {
                    { "Codigo", [validationException.Message] }
                });
            }
        }
    }
}
