using System.ComponentModel.DataAnnotations;

namespace Mercadito.Frontend.Dtos.Users;

public sealed class RequestPasswordResetRequestDto
{
    [Required(ErrorMessage = "El usuario o correo es obligatorio.")]
    [StringLength(100, ErrorMessage = "El usuario o correo no puede exceder 100 caracteres.")]
    public string Identifier { get; set; } = string.Empty;

    public string ResetUrlBase { get; set; } = string.Empty;
}
