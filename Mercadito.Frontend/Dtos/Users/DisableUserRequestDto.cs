using System.ComponentModel.DataAnnotations;

namespace Mercadito.Frontend.Dtos.Users;

public sealed class DisableUserRequestDto
{
    [Required(ErrorMessage = "La razón de baja es obligatoria.")]
    [StringLength(250, ErrorMessage = "La razón de baja no puede exceder 250 caracteres.")]
    public string Reason { get; set; } = string.Empty;
}