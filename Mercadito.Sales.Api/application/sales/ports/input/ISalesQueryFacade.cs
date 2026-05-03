using Mercadito.src.application.sales.models;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.sales.ports.input
{
    public interface ISalesQueryFacade
    {
        Task<Result<SalesRegistrationContext>> LoadRegistrationContextAsync(string customerSearchTerm = "", string productSearchTerm = "", CancellationToken cancellationToken = default);
        Task<Result<IReadOnlyList<CustomerLookupItem>>> SearchCustomersAsync(string customerSearchTerm = "", CancellationToken cancellationToken = default);
        Task<Result<IReadOnlyList<SaleProductOption>>> SearchProductsAsync(string productSearchTerm = "", CancellationToken cancellationToken = default);
        Task<Result<IReadOnlyList<SaleSummaryItem>>> GetRecentSalesAsync(int take = 20, string sortBy = "createdat", string sortDirection = "desc", CancellationToken cancellationToken = default);
        Task<Result<SalesOverviewMetrics>> GetOverviewMetricsAsync(CancellationToken cancellationToken = default);
        Task<Result<SaleDetailDto>> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default);
        Task<Result<SaleReceiptDto>> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default);
    }
}
