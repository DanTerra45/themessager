using System;
using System.ComponentModel.DataAnnotations;

namespace Mercadito
{
    public class CreateEmployeeDto
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es requerido")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El cargo es requerido")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de ingreso es requerida")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; }

        [Required(ErrorMessage = "El salario es requerido")]
        [Range(0, double.MaxValue, ErrorMessage = "El salario debe ser mayor o igual a cero")]
        public decimal Salary { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es requerido")]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
        public string Address { get; set; } = string.Empty;
    }
}