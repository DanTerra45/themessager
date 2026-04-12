using Mercadito.src.audit.domain.entities;
using Mercadito.src.sales.application.models;

namespace Mercadito.src.sales.application.ports.output
{
    public interface ISalesRepository
    {
        Task<string> GetNextSaleCodeAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CustomerLookupItem>> SearchCustomersAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SaleProductOption>> SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<long> RegisterAsync(RegisterSaleDto request, AuditActor actor, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SaleSummaryItem>> GetRecentSalesAsync(int take, string sortBy, string sortDirection, CancellationToken cancellationToken = default);
        Task<SalesOverviewMetrics> GetOverviewMetricsAsync(CancellationToken cancellationToken = default);
        Task<SaleDetailDto?> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default);
        Task<SaleReceiptDto?> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default);
        Task<bool> CancelAsync(long saleId, string reason, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
