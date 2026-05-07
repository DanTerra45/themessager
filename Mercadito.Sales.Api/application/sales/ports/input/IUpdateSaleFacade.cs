using Mercadito.Sales.Api.Application.Sales.Models;
using Mercadito.Sales.Api.Domain.Audit.Entities;
using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Sales.Ports.Input
{
    public interface IUpdateSaleFacade
    {
        Task<Result<SaleReceiptDto>> UpdateAsync(UpdateSaleDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
