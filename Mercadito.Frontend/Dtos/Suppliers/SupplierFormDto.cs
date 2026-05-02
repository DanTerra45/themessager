using System.ComponentModel.DataAnnotations;

namespace Mercadito.Frontend.Dtos.Suppliers;

public sealed class SupplierFormDto
{
    public long Id { get; set; }

    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener exactamente 6 caracteres.")]
    [RegularExpression("^PRV[0-9]{3}$", ErrorMessage = "El código debe tener formato PRV001.")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La razón social es obligatoria.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "La razón social debe tener entre 3 y 120 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(150, MinimumLength = 5, ErrorMessage = "La dirección debe tener entre 5 y 150 caracteres.")]
    public string Direccion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El contacto es obligatorio.")]
    [StringLength(60, MinimumLength = 3, ErrorMessage = "El contacto debe tener entre 3 y 60 caracteres.")]
    public string Contacto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rubro es obligatorio.")]
    [StringLength(50, MinimumLength = 4, ErrorMessage = "El rubro debe tener entre 4 y 50 caracteres.")]
    public string Rubro { get; set; } = string.Empty;

    public string? Telefono { get; set; }
}
