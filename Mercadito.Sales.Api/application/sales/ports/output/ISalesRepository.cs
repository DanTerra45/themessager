using Mercadito.Sales.Api.Domain.Audit.Entities;
using Mercadito.Sales.Api.Application.Sales.Models;

namespace Mercadito.Sales.Api.Application.Sales.Ports.Output
{
    public interface ISalesRepository
    {
        Task<string> GetNextSaleCodeAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CustomerLookupItem>> SearchCustomersAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SaleProductOption>> SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<long> RegisterAsync(RegisterSaleDto request, AuditActor actor, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(UpdateSaleDto request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SaleSummaryItem>> GetRecentSalesAsync(
            int take,
            string sortBy,
            string sortDirection,
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            string status = "",
            string paymentMethod = "",
            CancellationToken cancellationToken = default);
        Task<SalesOverviewMetrics> GetOverviewMetricsAsync(CancellationToken cancellationToken = default);
        Task<DailyCashClosingReport> GetDailyCashClosingReportAsync(DateOnly businessDate, CancellationToken cancellationToken = default);
        Task<SaleDetailDto?> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default);
        Task<SaleReceiptDto?> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default);
        Task<bool> CancelAsync(long saleId, string reason, AuditActor actor, CancellationToken cancellationToken = default);
    }
}
