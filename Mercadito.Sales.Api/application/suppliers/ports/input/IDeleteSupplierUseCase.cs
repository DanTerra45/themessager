using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Suppliers.Ports.Input
{
    public interface IDeleteSupplierUseCase
    {
        Task<Result<int>> ExecuteAsync(long id, CancellationToken cancellationToken = default);
    }
}
