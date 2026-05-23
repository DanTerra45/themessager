namespace MSProducto.Application.DTOs
{
    public class UpdateCategoriaDto
    {
        public long Id { get; set; }
        public required string Codigo { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
    }
}