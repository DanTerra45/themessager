using System.ComponentModel.DataAnnotations;

namespace Mercadito.Users.Api.Application.Users.Models
{
    public sealed class RequestPasswordResetDto
    {
        [Required(ErrorMessage = "El usuario o correo es obligatorio.")]
        [StringLength(100, ErrorMessage = "El usuario o correo no puede exceder 100 caracteres.")]
        [RegularExpression("^(?:[a-z0-9._-]{4,40}|[^\\s@]+@[^\\s@]+\\.[^\\s@]+)$", ErrorMessage = "Ingresa un usuario válido o un correo válido.")]
        public string Identifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "La URL de restablecimiento es obligatoria.")]
        [Url(ErrorMessage = "La URL de restablecimiento es inválida.")]
        public string ResetUrlBase { get; set; } = string.Empty;
    }
}
