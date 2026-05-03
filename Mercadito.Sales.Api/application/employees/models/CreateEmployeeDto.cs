using System.ComponentModel.DataAnnotations;
using Mercadito.src.application.shared.validation;

namespace Mercadito.src.application.employees.models
{
    public class CreateEmployeeDto
    {
        private const string HumanNamePattern = "^[A-Za-z\\u00C0-\\u024F]+(?:[ .'-][A-Za-z\\u00C0-\\u024F]+)*$";
        private const string ContactPattern = "^[0-9]{8}$";

        [CI(FieldName = "CI")]
        public long? Ci { get; set; }

        [StringLength(2, MinimumLength = 2, ErrorMessage = "El complemento debe tener exactamente 2 caracteres")]
        [RegularExpression("^[0-9][A-Za-z]$", ErrorMessage = "El complemento debe tener formato número+letra (ejemplo: 1A)")]
        public string? Complemento { get; set; }

        [Required(ErrorMessage = "Los nombres son requeridos")]
        [StringLength(40, MinimumLength = 2, ErrorMessage = "Los nombres deben tener entre 2 y 40 caracteres")]
        [RegularExpression(HumanNamePattern, ErrorMessage = "Los nombres solo permiten letras y separadores válidos (espacio, punto, apóstrofe o guion)")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "El primer apellido es requerido")]
        [StringLength(40, MinimumLength = 2, ErrorMessage = "El primer apellido debe tener entre 2 y 40 caracteres")]
        [RegularExpression(HumanNamePattern, ErrorMessage = "El primer apellido solo permite letras y separadores válidos (espacio, punto, apóstrofe o guion)")]
        public string PrimerApellido { get; set; } = string.Empty;

        [StringLength(40, ErrorMessage = "Máximo 40 caracteres")]
        [RegularExpression(HumanNamePattern, ErrorMessage = "El segundo apellido solo permite letras y separadores válidos (espacio, punto, apóstrofe o guion)")]
        public string? SegundoApellido { get; set; }

        [Required(ErrorMessage = "El cargo es requerido")]
        [RegularExpression("^(Cajero|Inventario)$", ErrorMessage = "El cargo debe ser Cajero o Inventario")]
        public string Cargo { get; set; } = "Cajero";

        [Required(ErrorMessage = "El número de contacto es requerido")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "El número de contacto debe tener exactamente 8 dígitos")]
        [RegularExpression(ContactPattern, ErrorMessage = "El número de contacto debe tener formato válido (ejemplo: 71234567)")]
        public string NumeroContacto { get; set; } = string.Empty;
    }
}
