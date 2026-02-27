using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Mercadito
{
    public class CreateProductDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre del Producto")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "La descripción es obligatoria")]
        [Display(Name = "Descripción del Producto")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string Description { get; set; }
        
        [Required(ErrorMessage = "El stock es obligatorio")]
        [Display(Name = "Stock Disponible")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser un número positivo")]
        [RegularExpression("^[0-9]+$", ErrorMessage = "El stock debe ser un número entero")]
        public int Stock { get; set; }
        
        [Required(ErrorMessage = "La fecha de lote es obligatoria")]
        [Display(Name = "Fecha del Lote")]
        [DataType(DataType.Date)]
        public DateTime Lote{ get; set; }
        
        [Required(ErrorMessage = "La fecha de caducidad es obligatoria")]
        [Display(Name = "Fecha de Caducidad")]
        [DateGreaterThan("Lote", ErrorMessage = "La fecha de caducidad debe ser posterior a la fecha del lote")]
        public DateTime FechaDeCaducidad { get; set; }
        
        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Price { get; set; }
        
        public CreateProductDto() { }
        
        public CreateProductDto(string name, string description, int stock, DateTime lote, DateTime fechaDeCaducidad, decimal price)
        {
            Name = name;
            Description = description;
            Stock = stock;
            Lote = lote;
            FechaDeCaducidad = fechaDeCaducidad;
            Price = price;
        }
    }

    // Validador personalizado para comparar fechas
    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateGreaterThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var currentValue = (DateTime)value;
            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);

            if (property == null)
                throw new ArgumentException($"Propiedad '{_comparisonProperty}' no encontrada");

            var comparisonValue = property.GetValue(validationContext.ObjectInstance);
            
            if (comparisonValue == null)
                return ValidationResult.Success;

            var comparisonDate = (DateTime)comparisonValue;

            if (currentValue <= comparisonDate)
                return new ValidationResult(ErrorMessage ?? "La fecha debe ser posterior");

            return ValidationResult.Success;
        }
    }
}

