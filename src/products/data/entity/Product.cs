namespace Mercadito.src.products.data.entity
{
    public class Product
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int Stock { get; set; }
        public DateOnly Batch { get; set; }
        public DateOnly ExpirationDate { get; set; }
        public decimal Price { get; set; }
    }
}