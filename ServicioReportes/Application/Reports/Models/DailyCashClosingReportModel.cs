namespace ServicioReportes.Application.Reports.Models;

public sealed record DailyCashClosingReportModel(
    DateOnly BusinessDate,
    DateTime GeneratedAt,
    int RegisteredSalesCount,
    int CancelledSalesCount,
    decimal RegisteredAmountTotal,
    decimal CancelledAmountTotal,
    decimal AverageTicket,
    IReadOnlyList<DailyPaymentMethodSummaryModel> PaymentMethods,
    IReadOnlyList<DailyProductSalesSummaryModel> Products);

public sealed record DailyPaymentMethodSummaryModel(
    string PaymentMethod,
    int SalesCount,
    decimal TotalAmount);

public sealed record DailyProductSalesSummaryModel(
    long ProductId,
    string ProductName,
    int QuantitySold,
    decimal AverageUnitPrice,
    decimal TotalAmount);
