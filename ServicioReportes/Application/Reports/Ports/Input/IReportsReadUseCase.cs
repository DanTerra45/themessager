using ServicioReportes.Application.Reports.Models;
using ServicioReportes.Domain.Shared;

namespace ServicioReportes.Application.Reports.Ports.Input;

public interface IReportsReadUseCase
{
    Task<Result<SalesMetricsModel>> GetMetricsAsync(CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SaleSummaryModel>>> GetRecentSalesAsync(
        int take = 20,
        string sortBy = "createdat",
        string sortDirection = "desc",
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string status = "",
        string paymentMethod = "",
        CancellationToken cancellationToken = default);

    Task<Result<DailyCashClosingReportModel>> GetDailyCashClosingReportAsync(DateOnly businessDate, CancellationToken cancellationToken = default);
    Task<Result<SaleDetailModel>> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default);
    Task<Result<SaleReceiptModel>> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default);
}
