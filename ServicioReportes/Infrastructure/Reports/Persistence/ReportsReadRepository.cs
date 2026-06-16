using Dapper;
using ServicioReportes.Application.Common.Ports;
using ServicioReportes.Application.Reports.Models;
using ServicioReportes.Application.Reports.Ports.Output;

namespace ServicioReportes.Infrastructure.Reports.Persistence;

public sealed class ReportsReadRepository(IReportsDbConnectionFactory connectionFactory) : IReportsReadRepository
{
    public async Task<SalesMetricsModel> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        const string query = @"
            SELECT
                COALESCE(SUM(CASE WHEN status <> 'Anulada' THEN 1 ELSE 0 END), 0)::int AS RegisteredSales,
                COALESCE(SUM(CASE WHEN status = 'Anulada' THEN 1 ELSE 0 END), 0)::int AS CancelledSales,
                COALESCE(SUM(CASE WHEN status <> 'Anulada' THEN total ELSE 0 END), 0) AS RegisteredAmount,
                COALESCE(SUM(CASE WHEN status = 'Anulada' THEN total ELSE 0 END), 0) AS CancelledAmount,
                COALESCE(SUM(CASE WHEN status <> 'Anulada' AND business_date = CURRENT_DATE THEN 1 ELSE 0 END), 0)::int AS SalesToday,
                COALESCE(SUM(CASE WHEN status <> 'Anulada' AND business_date = CURRENT_DATE THEN total ELSE 0 END), 0) AS SalesTodayAmount,
                COALESCE(AVG(CASE WHEN status <> 'Anulada' THEN total ELSE NULL END), 0) AS AverageTicket
            FROM sale_reports;";

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<SalesMetricsModel>(new CommandDefinition(query, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<SaleSummaryModel>> GetRecentSalesAsync(int take, string sortBy, string sortDirection, DateOnly? fromDate, DateOnly? toDate, string status, string paymentMethod, CancellationToken cancellationToken = default)
    {
        var orderBy = BuildOrderBy(sortBy, sortDirection);
        var fromDateValue = fromDate?.ToDateTime(TimeOnly.MinValue);
        var toDateValue = toDate?.ToDateTime(TimeOnly.MinValue);
        var query = $@"
            SELECT sale_id AS Id, code AS Code, created_at AS CreatedAt, customer_business_name AS CustomerName, channel AS Channel, payment_method AS PaymentMethod, total AS Total, status AS Status
            FROM sale_reports
            WHERE (CAST(@FromDate AS date) IS NULL OR business_date >= CAST(@FromDate AS date))
              AND (CAST(@ToDate AS date) IS NULL OR business_date <= CAST(@ToDate AS date))
              AND (@Status = '' OR status = @Status)
              AND (@PaymentMethod = '' OR payment_method = @PaymentMethod)
            ORDER BY {orderBy}
            LIMIT @Take;";

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<SaleSummaryModel>(new CommandDefinition(query, new { Take = take, FromDate = fromDateValue, ToDate = toDateValue, Status = status, PaymentMethod = paymentMethod }, cancellationToken: cancellationToken));
        return result.ToList();
    }

    public async Task<DailyCashClosingReportModel> GetDailyCashClosingReportAsync(DateOnly businessDate, CancellationToken cancellationToken = default)
    {
        const string totalsQuery = @"
            SELECT
                COALESCE(SUM(CASE WHEN status <> 'Anulada' THEN 1 ELSE 0 END), 0)::int AS RegisteredSalesCount,
                COALESCE(SUM(CASE WHEN status = 'Anulada' THEN 1 ELSE 0 END), 0)::int AS CancelledSalesCount,
                COALESCE(SUM(CASE WHEN status <> 'Anulada' THEN total ELSE 0 END), 0) AS RegisteredAmountTotal,
                COALESCE(SUM(CASE WHEN status = 'Anulada' THEN total ELSE 0 END), 0) AS CancelledAmountTotal,
                COALESCE(AVG(CASE WHEN status <> 'Anulada' THEN total ELSE NULL END), 0) AS AverageTicket
            FROM sale_reports
            WHERE business_date = @BusinessDate;";

        const string paymentMethodsQuery = @"
            SELECT payment_method AS PaymentMethod, COUNT(*)::int AS SalesCount, COALESCE(SUM(total), 0) AS TotalAmount
            FROM sale_reports
            WHERE business_date = @BusinessDate AND status <> 'Anulada'
            GROUP BY payment_method
            ORDER BY TotalAmount DESC, PaymentMethod ASC;";

        const string productsQuery = @"
            SELECT line.product_id AS ProductId, line.product_name AS ProductName, COALESCE(SUM(line.quantity), 0)::int AS QuantitySold, COALESCE(AVG(line.unit_price), 0) AS AverageUnitPrice, COALESCE(SUM(line.subtotal), 0) AS TotalAmount
            FROM sale_report_lines line
            INNER JOIN sale_reports report ON report.id = line.sale_report_id
            WHERE report.business_date = @BusinessDate AND report.status <> 'Anulada'
            GROUP BY line.product_id, line.product_name
            ORDER BY TotalAmount DESC, ProductName ASC;";

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var totals = await connection.QuerySingleAsync<DailyTotalsRow>(new CommandDefinition(totalsQuery, new { BusinessDate = businessDate }, cancellationToken: cancellationToken));
        var paymentMethods = (await connection.QueryAsync<DailyPaymentMethodSummaryModel>(new CommandDefinition(paymentMethodsQuery, new { BusinessDate = businessDate }, cancellationToken: cancellationToken))).ToList();
        var products = (await connection.QueryAsync<DailyProductSalesSummaryModel>(new CommandDefinition(productsQuery, new { BusinessDate = businessDate }, cancellationToken: cancellationToken))).ToList();

        return new DailyCashClosingReportModel(businessDate, DateTime.Now, totals.RegisteredSalesCount, totals.CancelledSalesCount, totals.RegisteredAmountTotal, totals.CancelledAmountTotal, totals.AverageTicket, paymentMethods, products);
    }

    public async Task<SaleDetailModel?> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default)
    {
        const string headerQuery = @"
            SELECT sale_id AS Id, code AS Code, created_at AS CreatedAt, customer_ci_nit AS CustomerCiNit, customer_business_name AS CustomerBusinessName, channel AS Channel, payment_method AS PaymentMethod, status AS Status, total AS Total
            FROM sale_reports WHERE sale_id = @SaleId;";
        const string linesQuery = @"
            SELECT line.product_id AS ProductId, line.product_name AS ProductName, COALESCE(line.lot_code, '') AS LotCode, line.quantity AS Quantity, line.unit_price AS UnitPrice, line.subtotal AS Subtotal
            FROM sale_report_lines line
            INNER JOIN sale_reports report ON report.id = line.sale_report_id
            WHERE report.sale_id = @SaleId
            ORDER BY line.id ASC;";

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var header = await connection.QuerySingleOrDefaultAsync<SaleDetailHeaderRow>(new CommandDefinition(headerQuery, new { SaleId = saleId }, cancellationToken: cancellationToken));
        if (header is null) return null;

        var lines = (await connection.QueryAsync<SaleDetailLineModel>(new CommandDefinition(linesQuery, new { SaleId = saleId }, cancellationToken: cancellationToken))).ToList();
        return new SaleDetailModel(header.Id, header.Code, header.CreatedAt, header.CustomerCiNit, header.CustomerBusinessName, header.Channel, header.PaymentMethod, header.Status, header.Total, lines);
    }

    public async Task<SaleReceiptModel?> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default)
    {
        const string headerQuery = @"
            SELECT sale_id AS Id, code AS Code, created_at AS CreatedAt, generated_at AS GeneratedAt, customer_ci_nit AS CustomerCiNit, customer_business_name AS CustomerBusinessName, created_by_username AS CreatedBy, total AS Total, amount_in_words AS AmountInWords
            FROM sale_reports WHERE sale_id = @SaleId;";
        const string linesQuery = @"
            SELECT line.product_name AS ProductName, line.quantity AS Quantity, line.unit_price AS UnitPrice, line.subtotal AS Subtotal
            FROM sale_report_lines line
            INNER JOIN sale_reports report ON report.id = line.sale_report_id
            WHERE report.sale_id = @SaleId
            ORDER BY line.id ASC;";

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var header = await connection.QuerySingleOrDefaultAsync<SaleReceiptHeaderRow>(new CommandDefinition(headerQuery, new { SaleId = saleId }, cancellationToken: cancellationToken));
        if (header is null) return null;

        var lines = (await connection.QueryAsync<SaleReceiptLineModel>(new CommandDefinition(linesQuery, new { SaleId = saleId }, cancellationToken: cancellationToken))).ToList();
        return new SaleReceiptModel(header.Id, header.Code, header.CreatedAt, header.GeneratedAt, header.CustomerCiNit, header.CustomerBusinessName, header.CreatedBy, header.Total, header.AmountInWords, lines);
    }

    private static string BuildOrderBy(string sortBy, string sortDirection)
    {
        var direction = string.Equals(sortDirection, "asc", StringComparison.Ordinal) ? "ASC" : "DESC";
        var reverseDirection = direction == "ASC" ? "DESC" : "ASC";

        return sortBy switch
        {
            "code" => $"code {direction}, sale_id {reverseDirection}",
            "customer" => $"customer_business_name {direction}, sale_id {reverseDirection}",
            "paymentmethod" => $"payment_method {direction}, sale_id {reverseDirection}",
            "total" => $"total {direction}, sale_id {reverseDirection}",
            "status" => $"CASE WHEN status = 'Anulada' THEN 1 ELSE 0 END {direction}, sale_id {reverseDirection}",
            _ => $"created_at {direction}, sale_id {reverseDirection}"
        };
    }

    private sealed record DailyTotalsRow(int RegisteredSalesCount, int CancelledSalesCount, decimal RegisteredAmountTotal, decimal CancelledAmountTotal, decimal AverageTicket);
    private sealed record SaleDetailHeaderRow(long Id, string Code, DateTime CreatedAt, string CustomerCiNit, string CustomerBusinessName, string Channel, string PaymentMethod, string Status, decimal Total);
    private sealed record SaleReceiptHeaderRow(long Id, string Code, DateTime CreatedAt, DateTime GeneratedAt, string CustomerCiNit, string CustomerBusinessName, string CreatedBy, decimal Total, string AmountInWords);
}
