using System.ComponentModel.DataAnnotations;
using Mercadito.Frontend.Validation;

namespace Mercadito.Frontend.Dtos.Products;

public sealed class ProductFormDto
{
    public long Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
    [RegularExpression(@"^[\p{L}\p{N}\s\.\-_,]+$", ErrorMessage = "El nombre solo permite letras, números, espacios y los caracteres . - _ ,")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria")]
    [StringLength(150, ErrorMessage = "La descripción no puede exceder 150 caracteres")]
    public string Description { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser un número positivo")]
    public int? Stock { get; set; } = 0;

    [Required(ErrorMessage = "Lote es obligatorio")]
    [StringLength(40, ErrorMessage = "Lote no puede exceder 40 caracteres")]
    [RegularExpression("^[0-9]{1,40}$", ErrorMessage = "El lote solo permite números")]
    public string Batch { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de caducidad es obligatoria")]
    public DateOnly ExpirationDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(3));

    [MinimumDecimal(0.01, ErrorMessage = "El Precio debe ser un número decimal positivo")]
    public decimal? Price { get; set; } = 0.01m;

    [MinLength(1, ErrorMessage = "Debe seleccionar al menos una categoría")]
    public List<long> CategoryIds { get; set; } = [];
}
