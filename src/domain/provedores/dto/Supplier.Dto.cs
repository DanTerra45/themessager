namespace Mercadito.src.domain.provedores.dto
{
    public class CreateSupplierDto
    {
        public required string Codigo { get; set; }
        public required string Nombre { get; set; }
        public required string Direccion { get; set; }
        public required string Contacto { get; set; }
        public required string Rubro { get; set; }
        public string? Telefono { get; set; }
    }
    public class UpdateSupplierDto
    {
        public long Id { get; set; }
        public required string Codigo { get; set; }
        public required string Nombre { get; set; }
        public required string Direccion { get; set; }
        public required string Contacto { get; set; }
        public required string Rubro { get; set; }
        public string? Telefono { get; set; }
    }
    public class SupplierDto
    {
        public long Id { get; set; }
        public required string Codigo { get; set; }
        public required string Nombre { get; set; }
        public required string Direccion { get; set; }
        public required string Contacto { get; set; }
        public required string Rubro { get; set; }
    }
}