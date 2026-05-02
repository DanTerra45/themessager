namespace Mercadito.src.domain.products.entities
{
    public class Product
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int Stock { get; set; }
        public required string Batch { get; set; }
        public DateOnly ExpirationDate { get; set; }
        public decimal Price { get; set; }
        public required IReadOnlyList<long> CategoryIds { get; set; }
    }
}
