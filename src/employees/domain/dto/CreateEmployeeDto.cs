using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.employees.domain.dto
{
    public class CreateEmployeeDto
    {
        [Required(ErrorMessage = "El CI es requerido")]
        [Range(1, long.MaxValue, ErrorMessage = "El CI debe ser mayor a cero")]
        public long Ci { get; set; }

        [StringLength(20, ErrorMessage = "Maximo 20 caracteres")]
        public string? Complemento { get; set; }

        [Required(ErrorMessage = "Los nombres son requeridos")]
        [StringLength(40, MinimumLength = 2, ErrorMessage = "Los nombres deben tener entre 2 y 40 caracteres")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "El primer apellido es requerido")]
        [StringLength(40, MinimumLength = 2, ErrorMessage = "El primer apellido debe tener entre 2 y 40 caracteres")]
        public string PrimerApellido { get; set; } = string.Empty;

        [StringLength(40, ErrorMessage = "Maximo 40 caracteres")]
        public string? SegundoApellido { get; set; }

        [Required(ErrorMessage = "El rol es requerido")]
        [RegularExpression("^(Cajero|Inventario)$", ErrorMessage = "El rol debe ser Cajero o Inventario")]
        public string Rol { get; set; } = "Cajero";

        [Required(ErrorMessage = "El numero de contacto es requerido")]
        [StringLength(40, MinimumLength = 7, ErrorMessage = "El numero de contacto debe tener entre 7 y 40 caracteres")]
        public string NumeroContacto { get; set; } = string.Empty;
    }
}