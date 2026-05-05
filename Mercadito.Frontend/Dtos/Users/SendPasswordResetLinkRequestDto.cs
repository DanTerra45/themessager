using System.ComponentModel.DataAnnotations;

namespace Mercadito.Frontend.Dtos.Users;

public sealed class SendPasswordResetLinkRequestDto
{
    [Range(1, long.MaxValue, ErrorMessage = "El usuario es inválido.")]
    public long UserId { get; set; }

    [Required(ErrorMessage = "El usuario es obligatorio.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Email { get; set; } = string.Empty;

    public string ResetUrlBase { get; set; } = string.Empty;
}
