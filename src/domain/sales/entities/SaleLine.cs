namespace Mercadito.src.domain.sales.entities
{
    public sealed class SaleLine
    {
        public long ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal Amount { get; init; }
    }
}
