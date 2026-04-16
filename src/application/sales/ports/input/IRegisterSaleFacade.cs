using Mercadito.src.application.sales.models;
using Mercadito.src.domain.audit.entities;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.sales.ports.input
{
    public interface IRegisterSaleFacade
    {
        Task<Result<SaleReceiptDto>> RegisterAsync(RegisterSaleDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
