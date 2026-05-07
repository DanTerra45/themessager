namespace Mercadito.Sales.Api.Contracts.Sales;

public sealed record DailyCashClosingReportResponse(
    DateOnly BusinessDate,
    DateTime GeneratedAt,
    int RegisteredSalesCount,
    int CancelledSalesCount,
    decimal RegisteredAmountTotal,
    decimal CancelledAmountTotal,
    decimal AverageTicket,
    IReadOnlyList<DailyPaymentMethodSummaryResponse> PaymentMethods,
    IReadOnlyList<DailyProductSalesSummaryResponse> Products);

public sealed record DailyPaymentMethodSummaryResponse(
    string PaymentMethod,
    int SalesCount,
    decimal TotalAmount);

public sealed record DailyProductSalesSummaryResponse(
    long ProductId,
    string ProductName,
    int QuantitySold,
    decimal AverageUnitPrice,
    decimal TotalAmount);
