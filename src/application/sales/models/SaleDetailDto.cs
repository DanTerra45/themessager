namespace Mercadito.src.application.sales.models
{
    public sealed class SaleDetailDto
    {
        public long Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public long CustomerId { get; init; }
        public string CustomerDocumentNumber { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public string PaymentMethod { get; init; } = string.Empty;
        public string Channel { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string CreatedByUsername { get; init; } = string.Empty;
        public string CancellationReason { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime? CancelledAt { get; init; }
        public decimal Total { get; init; }
        public IReadOnlyList<SaleDetailLineDto> Lines { get; init; } = [];
    }
}
