using System.ComponentModel.DataAnnotations;

namespace Mercadito.Users.Api.Application.Users.Models
{
    public sealed class LoginUserCommand
    {
        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [StringLength(40, MinimumLength = 4, ErrorMessage = "El usuario debe tener entre 4 y 40 caracteres.")]
        [RegularExpression("^[a-z0-9._-]{4,40}$", ErrorMessage = "El usuario solo admite minúsculas, números, punto, guion y guion bajo.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(128, ErrorMessage = "La contraseña no puede exceder 128 caracteres.")]
        public string Password { get; set; } = string.Empty;
    }
}
