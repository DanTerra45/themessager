using System.ComponentModel.DataAnnotations;

namespace Mercadito.Frontend.Dtos.Users;

public sealed class CompletePasswordResetRequestDto
{
    [Required(ErrorMessage = "El token de restablecimiento es obligatorio.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 128 caracteres.")]
    [RegularExpression("^(?=.*[A-Z])(?=.*[a-z])(?=.*\\d).+$", ErrorMessage = "La contraseña debe incluir al menos una letra mayúscula, una minúscula y un número.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
    [Compare(nameof(Password), ErrorMessage = "La confirmación no coincide con la contraseña.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
