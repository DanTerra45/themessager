using Mercadito.Sales.Api.Application.Suppliers.Models;
using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Suppliers.Ports.Input
{
    public interface IGetAllSuppliersUseCase
    {
        Task<Result<IReadOnlyList<SupplierDto>>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
