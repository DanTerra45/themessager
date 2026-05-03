using Mercadito.src.application.sales.models;
using Mercadito.src.domain.audit.entities;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.sales.ports.input
{
    public interface ICancelSaleFacade
    {
        Task<Result<bool>> CancelAsync(CancelSaleDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
