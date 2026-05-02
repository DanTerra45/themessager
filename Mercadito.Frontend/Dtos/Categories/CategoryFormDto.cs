using System.ComponentModel.DataAnnotations;

namespace Mercadito.Frontend.Dtos.Categories;

public sealed class CategoryFormDto
{
    public long Id { get; set; }

    [Required(ErrorMessage = "El código es obligatorio")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener exactamente 6 caracteres")]
    [RegularExpression("^C[0-9]{5}$", ErrorMessage = "El código debe tener formato C00001")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria")]
    [StringLength(150, ErrorMessage = "La descripción no puede exceder 150 caracteres")]
    public string Description { get; set; } = string.Empty;
}
