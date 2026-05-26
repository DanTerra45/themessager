using System.ComponentModel.DataAnnotations;

namespace Mercadito.Frontend.Dtos.Users;

public sealed class RequestPasswordResetRequestDto
{
    [Required(ErrorMessage = "El usuario o correo es obligatorio.")]
    [StringLength(100, ErrorMessage = "El usuario o correo no puede exceder 100 caracteres.")]
    public string EmailOrUsername { get; set; } = string.Empty;
}
