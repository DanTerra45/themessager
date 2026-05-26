using Mercadito.Sales.Api.Application.Sales.Models;
using Mercadito.Sales.Api.Domain.Audit.Entities;
using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Sales.Ports.Input
{
    public interface IRegisterSaleFacade
    {
        Task<Result<SaleReceiptDto>> RegisterAsync(RegisterSaleDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
