namespace Domain.Dto.Response;

public sealed record SalesReportResponseDto(
    DateTime GeneratedAt,
    int TotalSalesCount,
    int PendingSalesCount,
    int ConfirmedSalesCount,
    int CancelledSalesCount,
    decimal TotalAmount,
    decimal CancelledAmount,
    decimal AverageTicket,
    IReadOnlyList<SalesReportProductDto> Products);

public sealed record SalesReportProductDto(
    int ProductId,
    int Quantity,
    decimal Amount);