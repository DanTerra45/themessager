using System.ComponentModel.DataAnnotations;

namespace Mercadito
{
    public class CreateEmployeeDto
    {
        [Required(ErrorMessage = "El CI es requerido")]
        public long Ci { get; set; }

        public string? Complemento { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "El primer apellido es requerido")]
        public string PrimerApellido { get; set; } = string.Empty;

        public string? SegundoApellido { get; set; }

        [Required(ErrorMessage = "El rol es requerido")]
        public string Rol { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de contacto es requerido")]
        public string NumeroContacto { get; set; } = string.Empty;

    }
}