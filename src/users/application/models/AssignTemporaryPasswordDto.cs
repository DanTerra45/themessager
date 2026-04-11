using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.users.application.models
{
    public sealed class AssignTemporaryPasswordDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "El usuario es inválido.")]
        public long UserId { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña temporal es obligatoria.")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "La contraseña temporal debe tener entre 8 y 128 caracteres.")]
        [RegularExpression("^(?=.*[A-Z])(?=.*[a-z])(?=.*\\d).+$", ErrorMessage = "La contraseña temporal debe incluir al menos una letra mayúscula, una minúscula y un número.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [Compare(nameof(Password), ErrorMessage = "La confirmación no coincide con la contraseña.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
