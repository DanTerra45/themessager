using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.application.users.models
{
    public sealed class CreateUserDto
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

        [Required(ErrorMessage = "La URL de activación es obligatoria.")]
        [Url(ErrorMessage = "La URL de activación es inválida.")]
        public string SetupUrlBase { get; set; } = string.Empty;
    }
}
