using Mercadito.Sales.Api.Application.Suppliers.Models;
using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Suppliers.Ports.Input
{
    public interface IGetSupplierByIdUseCase
    {
        Task<Result<SupplierDto>> ExecuteAsync(long id, CancellationToken cancellationToken = default);
    }
}
