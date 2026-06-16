using ServicioReportes.Application.Reports.Models;

namespace ServicioReportes.Application.Reports.Ports.Output;

public interface IReportsReadRepository
{
    Task<SalesMetricsModel> GetMetricsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SaleSummaryModel>> GetRecentSalesAsync(int take, string sortBy, string sortDirection, DateOnly? fromDate, DateOnly? toDate, string status, string paymentMethod, CancellationToken cancellationToken = default);
    Task<DailyCashClosingReportModel> GetDailyCashClosingReportAsync(DateOnly businessDate, CancellationToken cancellationToken = default);
    Task<SaleDetailModel?> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default);
    Task<SaleReceiptModel?> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default);
}
