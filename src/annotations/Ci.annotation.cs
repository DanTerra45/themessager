using System;
using System.ComponentModel.DataAnnotations;

namespace Mercadito
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class CIAttribute : ValidationAttribute
    {
        [Required(ErrorMessage = "El {0} es obligatorio")]
        public required string FieldName { get; set; }

        private static bool IsValidLength(long value)
        {
            return value > 99999 && value <= 9999999999L;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            return value switch
            {
                null => new ValidationResult($"El {FieldName} es obligatorio"),
                long l when !IsValidLength(l) => new ValidationResult($"El {FieldName} debe tener entre 6 y 10 dígitos"),
                long => ValidationResult.Success,
                _ => new ValidationResult("El atributo CI solo se puede aplicar a propiedades de tipo long")
            };
        }
    }
}