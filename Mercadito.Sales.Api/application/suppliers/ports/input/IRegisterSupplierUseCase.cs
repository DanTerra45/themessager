using Mercadito.Sales.Api.Application.Suppliers.Models;
using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Suppliers.Ports.Input
{
    public interface IRegisterSupplierUseCase
    {
        Task<Result<long>> ExecuteAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default);
    }
}
