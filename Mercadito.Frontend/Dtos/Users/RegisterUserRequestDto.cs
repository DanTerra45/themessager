using System.ComponentModel.DataAnnotations;

namespace Mercadito.Frontend.Dtos.Users;

public sealed class RegisterUserRequestDto
{
    [StringLength(40, ErrorMessage = "El usuario generado no puede exceder 40 caracteres.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [StringLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [Range(1, long.MaxValue, ErrorMessage = "El empleado asociado es inválido.")]
    public long? EmployeeId { get; set; }

    [Required(ErrorMessage = "El rol es obligatorio.")]
    [RegularExpression("^(Admin|Operador|Auditor)$", ErrorMessage = "El rol debe ser Admin, Operador o Auditor.")]
    public string Role { get; set; } = string.Empty;

    public string SetupUrlBase { get; set; } = string.Empty;
}
