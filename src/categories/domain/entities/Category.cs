namespace Mercadito.src.categories.domain.entities
{
    public class Category
    {
        public long Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
    }
}
