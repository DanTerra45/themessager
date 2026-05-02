namespace Mercadito.src.domain.sales.entities
{
    public sealed class Customer
    {
        public long Id { get; init; }
        public string DocumentNumber { get; init; } = string.Empty;
        public string BusinessName { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
    }
}
