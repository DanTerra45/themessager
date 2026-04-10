using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.users.application.models
{
    public sealed class SendAdministrativePasswordResetLinkDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "El usuario es inválido.")]
        public long UserId { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [StringLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La URL de restablecimiento es obligatoria.")]
        [Url(ErrorMessage = "La URL de restablecimiento es inválida.")]
        public string ResetUrlBase { get; set; } = string.Empty;
    }
}
