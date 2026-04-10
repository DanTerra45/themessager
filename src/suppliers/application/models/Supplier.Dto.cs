using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.suppliers.application.models
{
    public class CreateSupplierDto
    {
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La razón social es obligatoria.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        public string Direccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El contacto es obligatorio.")]
        public string Contacto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rubro es obligatorio.")]
        public string Rubro { get; set; } = string.Empty;

        public string? Telefono { get; set; }
    }
    public class UpdateSupplierDto
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "El código es obligatorio.")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La razón social es obligatoria.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        public string Direccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El contacto es obligatorio.")]
        public string Contacto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rubro es obligatorio.")]
        public string Rubro { get; set; } = string.Empty;

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
        public string Telefono { get; set; } = string.Empty;
    }
}
