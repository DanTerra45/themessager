using System.ComponentModel.DataAnnotations;

namespace ServicioFrontend.Dtos.Users;

public sealed class ChangePasswordRequestDto
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 128 caracteres.")]
    [RegularExpression("^(?=.*[A-Z])(?=.*[a-z])(?=.*\\d).+$", ErrorMessage = "La contraseña debe incluir al menos una letra mayúscula, una minúscula y un número.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
    [Compare(nameof(NewPassword), ErrorMessage = "La confirmación no coincide con la nueva contraseña.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
