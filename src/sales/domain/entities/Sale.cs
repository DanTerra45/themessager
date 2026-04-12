namespace Mercadito.src.sales.domain.entities
{
    public sealed class Sale
    {
        public long Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public long CustomerId { get; init; }
        public long UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Channel { get; init; } = string.Empty;
        public string PaymentMethod { get; init; } = string.Empty;
        public SaleStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? CancelledAt { get; init; }
        public string CancellationReason { get; init; } = string.Empty;
        public decimal Total { get; init; }
        public IReadOnlyList<SaleLine> Lines { get; init; } = [];
    }
}
