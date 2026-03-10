namespace Mercadito.src.products.data.entity
{
    public class Product
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int Stock { get; set; }
        public DateTime Batch { get; set; }
        public DateTime ExpirationDate { get; set; }
        public decimal Price { get; set; }
        public Product() { }
        public Product(long id, string name, string description, int stock, DateTime batch, DateTime expirationDate, decimal price)
        {
            Id = id;
            Name = name;
            Description = description;
            Stock = stock;
            Batch = batch;
            ExpirationDate = expirationDate;
            Price = price;
        }
    }
}