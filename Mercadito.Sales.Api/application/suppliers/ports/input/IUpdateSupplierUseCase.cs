using Mercadito.Sales.Api.Application.Suppliers.Models;
using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Suppliers.Ports.Input
{
    public interface IUpdateSupplierUseCase
    {
        Task<Result<int>> ExecuteAsync(UpdateSupplierDto dto, CancellationToken cancellationToken = default);
    }
}
