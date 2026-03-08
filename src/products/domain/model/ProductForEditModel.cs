namespace Mercadito.src.products.domain.model
{
    public class ProductForEditModel
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int Stock { get; set; }
        public DateTime Batch { get; set; }
        public DateTime ExpirationDate { get; set; }
        public decimal Price { get; set; }
        public long CategoryId { get; set; }
    }
}