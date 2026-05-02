namespace Mercadito.src.application.categories.models
{
    public class CategoryModel
    {
        public long Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int ProductCount { get; set; }
    }
}
