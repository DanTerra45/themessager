using Mercadito.src.application.sales.models;
using Mercadito.src.domain.audit.entities;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.sales.ports.input
{
    public interface IUpdateSaleFacade
    {
        Task<Result<SaleReceiptDto>> UpdateAsync(UpdateSaleDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
