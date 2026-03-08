namespace Mercadito.src.products.domain.model
{
    public class ProductWithCategoriesModel
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int Stock { get; set; }
        public DateTime Batch { get; set; }
        public DateTime ExpirationDate { get; set; }
        public decimal Price { get; set; }
        public List<string> Categories { get; set; } = [];
        public ProductWithCategoriesModel() { }
        public ProductWithCategoriesModel(long id, string name, string description, int stock, DateTime batch, DateTime expirationDate, decimal price)
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