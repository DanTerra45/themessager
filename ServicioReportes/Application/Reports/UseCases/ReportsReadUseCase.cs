using ServicioReportes.Application.Reports.Models;
using ServicioReportes.Application.Reports.Ports.Input;
using ServicioReportes.Application.Reports.Ports.Output;
using ServicioReportes.Domain.Shared;

namespace ServicioReportes.Application.Reports.UseCases;

public sealed class ReportsReadUseCase(IReportsReadRepository reportsReadRepository) : IReportsReadUseCase
{
    private static readonly IReadOnlySet<string> AllowedSortFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "code", "createdat", "customer", "paymentmethod", "total", "status"
    };

    public async Task<Result<SalesMetricsModel>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = await reportsReadRepository.GetMetricsAsync(cancellationToken);
        return Result.Success(metrics);
    }

    public async Task<Result<IReadOnlyList<SaleSummaryModel>>> GetRecentSalesAsync(int take = 20, string sortBy = "createdat", string sortDirection = "desc", DateOnly? fromDate = null, DateOnly? toDate = null, string status = "", string paymentMethod = "", CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (take <= 0 || take > 500)
        {
            errors["take"] = ["La cantidad de registros solicitada es inválida."];
        }

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            errors["dateRange"] = ["La fecha desde no puede ser mayor a la fecha hasta."];
        }

        var normalizedSortBy = NormalizeSortBy(sortBy);
        if (!AllowedSortFields.Contains(normalizedSortBy))
        {
            errors["sortBy"] = ["El criterio de ordenamiento es inválido."];
        }

        var normalizedSortDirection = NormalizeSortDirection(sortDirection);
        if (normalizedSortDirection is not "asc" and not "desc")
        {
            errors["sortDirection"] = ["La dirección de ordenamiento es inválida."];
        }

        if (errors.Count > 0)
        {
            return Result.Failure<IReadOnlyList<SaleSummaryModel>>(errors);
        }

        var sales = await reportsReadRepository.GetRecentSalesAsync(
            take,
            normalizedSortBy,
            normalizedSortDirection,
            fromDate,
            toDate,
            NormalizeStatus(status),
            NormalizeText(paymentMethod),
            cancellationToken);

        return Result.Success(sales);
    }

    public async Task<Result<DailyCashClosingReportModel>> GetDailyCashClosingReportAsync(DateOnly businessDate, CancellationToken cancellationToken = default)
    {
        var normalizedDate = businessDate == default ? DateOnly.FromDateTime(DateTime.Today) : businessDate;
        var report = await reportsReadRepository.GetDailyCashClosingReportAsync(normalizedDate, cancellationToken);
        return Result.Success(report);
    }

    public async Task<Result<SaleDetailModel>> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default)
    {
        if (saleId <= 0)
        {
            return Result.Failure<SaleDetailModel>(new Dictionary<string, List<string>> { ["saleId"] = ["El identificador de la venta es inválido."] });
        }

        var sale = await reportsReadRepository.GetSaleDetailAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result.Failure<SaleDetailModel>("No se encontró el detalle de la venta solicitada.");
        }

        return Result.Success(sale);
    }

    public async Task<Result<SaleReceiptModel>> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default)
    {
        if (saleId <= 0)
        {
            return Result.Failure<SaleReceiptModel>(new Dictionary<string, List<string>> { ["saleId"] = ["El identificador de la venta es inválido."] });
        }

        var receipt = await reportsReadRepository.GetSaleReceiptAsync(saleId, cancellationToken);
        if (receipt is null)
        {
            return Result.Failure<SaleReceiptModel>("No se encontró el comprobante solicitado.");
        }

        return Result.Success(receipt);
    }

    private static string NormalizeSortBy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "createdat";
        }

        return value.Trim().ToLowerInvariant();
    }

    private static string NormalizeSortDirection(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "desc";
        }

        return value.Trim().ToLowerInvariant();
    }

    private static string NormalizeStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "anulada" => "Anulada",
            "registrada" => "Registrada",
            _ => string.Empty
        };
    }

    private static string NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
