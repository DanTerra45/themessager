using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.application.sales.models
{
    public sealed class CreateCustomerDto
    {
        [Required(ErrorMessage = "El CI/NIT es obligatorio.")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "El CI/NIT debe tener entre 1 y 20 caracteres.")]
        [RegularExpression("^([0-9A-Za-z-]{5,20}|0)$", ErrorMessage = "El CI/NIT debe ser 0 o tener entre 5 y 20 caracteres válidos.")]
        public string DocumentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "La razón social es obligatoria.")]
        [StringLength(150, ErrorMessage = "La razón social no puede exceder 150 caracteres.")]
        public string BusinessName { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres.")]
        public string? Phone { get; set; }

        [StringLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        public string? Email { get; set; }

        [StringLength(150, ErrorMessage = "La dirección no puede exceder 150 caracteres.")]
        public string? Address { get; set; }
    }
}
