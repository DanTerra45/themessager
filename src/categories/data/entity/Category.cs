namespace Mercadito.src.categories.data.entity
{
    public class Category
    {
        public long Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
    }
}