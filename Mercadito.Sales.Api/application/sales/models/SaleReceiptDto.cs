namespace Mercadito.src.application.sales.models
{
    public sealed class SaleReceiptDto
    {
        public long Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime GeneratedAt { get; init; }
        public string CustomerDocumentNumber { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public string CreatedByUsername { get; init; } = string.Empty;
        public decimal Total { get; init; }
        public string AmountInWords { get; init; } = string.Empty;
        public IReadOnlyList<SaleReceiptLineDto> Lines { get; init; } = [];
    }
}
