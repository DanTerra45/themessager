namespace Mercadito.src.domain.suppliers.entities
{
    public class Supplier
    {
        public long? Id { get; set; }
        public required string Codigo { get; set; }
        public required string RazonSocial { get; set; }
        public required string Direccion { get; set; }
        public required string Contacto { get; set; }
        public required string Rubro { get; set; }
        public required string Telefono { get; set; }
    }
}
