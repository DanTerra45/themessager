using System;
using System.ComponentModel.DataAnnotations;

namespace Mercadito
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PositiveAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            return value switch
            {
                null => new ValidationResult("El valor es obligatorio"),
                int i when i < 0 => new ValidationResult("El valor debe ser un número entero positivo"),
                int => ValidationResult.Success,
                decimal d when d <= 0 => new ValidationResult("El valor debe ser un número decimal positivo"),
                decimal => ValidationResult.Success,
                _ => new ValidationResult("El atributo Positive solo se puede aplicar a propiedades de tipo int o decimal")
            };
        }
    }
}