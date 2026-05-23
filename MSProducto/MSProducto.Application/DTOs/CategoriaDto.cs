namespace MSProducto.Application.DTOs
{
    public class CategoriaDto
    {
        public long Id { get; set; }
        public required string Codigo { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
        public int ProductosActivosCount { get; set; }
        public bool Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime UltimaActualizacion { get; set; }
    }
}