namespace MSProducto.Application.DTOs
{
    public class ProductoDto
    {
        public long Id { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
        public required string Lote { get; set; }
        public DateOnly FechaCaducidad { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public bool Estado { get; set; }
        public bool ActivoUnico { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime UltimaActualizacion { get; set; }
        public IReadOnlyList<string> Categorias { get; set; } = [];
    }
}