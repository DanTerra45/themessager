using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Suppliers.Ports.Input
{
    public interface IGetNextSupplierCodeUseCase
    {
        Task<Result<string>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
