using Mercadito.Sales.Api.Application.Sales.Models;
using Mercadito.Sales.Api.Domain.Audit.Entities;
using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Sales.Ports.Input
{
    public interface ICancelSaleFacade
    {
        Task<Result<bool>> CancelAsync(CancelSaleDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
