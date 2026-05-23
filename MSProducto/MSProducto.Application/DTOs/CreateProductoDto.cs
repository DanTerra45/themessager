namespace MSProducto.Application.DTOs
{
    public class CreateProductoDto
    {
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
        public required string Lote { get; set; }
        public DateOnly FechaCaducidad { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public IReadOnlyList<long> CategoriaIds { get; set; } = [];
    }
}