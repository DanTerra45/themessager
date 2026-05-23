namespace MSProducto.Domain.Entities
{
    public class Categoria
    {
        public long Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
    }
}