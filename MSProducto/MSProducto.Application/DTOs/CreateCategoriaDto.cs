namespace MSProducto.Application.DTOs
{
    public class CreateCategoriaDto
    {
        public required string Codigo { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
    }
}