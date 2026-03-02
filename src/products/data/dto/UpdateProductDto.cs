
using System;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.products.data.dto
{
    public class UpdateProductDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string Name { get; set; } = string.Empty;

        public Guid CategoryId { get; set; } = Guid.Empty;

        [StringLength(2000, ErrorMessage = "La descripción no puede exceder 2000 caracteres")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Stock es obligatorio")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock debe ser 0 o mayor")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Lote es obligatorio")]
        [DataType(DataType.Date)]
        public DateTime Lote { get; set; }

        [Required(ErrorMessage = "Fecha de vencimiento es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime FechaDeCaducidad { get; set; }

        [Required(ErrorMessage = "Precio es obligatorio")]
        [Range(0, double.MaxValue, ErrorMessage = "Precio inválido")]
        public decimal Price { get; set; }
    }
}