namespace Mercadito.Frontend.Dtos.Sales;

public sealed record DailyCashClosingReportDto(
    DateOnly BusinessDate,
    DateTime GeneratedAt,
    int RegisteredSalesCount,
    int CancelledSalesCount,
    decimal RegisteredAmountTotal,
    decimal CancelledAmountTotal,
    decimal AverageTicket,
    IReadOnlyList<DailyPaymentMethodSummaryDto> PaymentMethods,
    IReadOnlyList<DailyProductSalesSummaryDto> Products);

public sealed record DailyPaymentMethodSummaryDto(
    string PaymentMethod,
    int SalesCount,
    decimal TotalAmount);

public sealed record DailyProductSalesSummaryDto(
    long ProductId,
    string ProductName,
    int QuantitySold,
    decimal AverageUnitPrice,
    decimal TotalAmount);
