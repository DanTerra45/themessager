using Mercadito.src.domain.audit.entities;
using Mercadito.src.application.sales.models;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.sales.ports.input
{
    public interface ISalesTransactionFacade
    {
        Task<Result<SalesRegistrationContext>> LoadRegistrationContextAsync(string customerSearchTerm = "", string productSearchTerm = "", CancellationToken cancellationToken = default);
        Task<Result<SaleReceiptDto>> RegisterAsync(RegisterSaleDto request, AuditActor actor, CancellationToken cancellationToken = default);
        Task<Result<IReadOnlyList<SaleSummaryItem>>> GetRecentSalesAsync(int take = 20, string sortBy = "createdat", string sortDirection = "desc", CancellationToken cancellationToken = default);
        Task<Result<SalesOverviewMetrics>> GetOverviewMetricsAsync(CancellationToken cancellationToken = default);
        Task<Result<SaleDetailDto>> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default);
        Task<Result<SaleReceiptDto>> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default);
        Task<Result<bool>> CancelAsync(CancelSaleDto request, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
