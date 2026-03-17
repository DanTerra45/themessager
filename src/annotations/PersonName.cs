using System;
using System.ComponentModel.DataAnnotations;

namespace Mercadito
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PersonNameAttribute : ValidationAttribute
    {
        [Required(ErrorMessage = "El {0} es obligatorio")]
        public required string FieldName { get; set; }
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            return value switch
            {
                null => new ValidationResult($"El {FieldName} es obligatorio"),
                string s when string.IsNullOrWhiteSpace(s) => new ValidationResult($"El {FieldName} es obligatorio"),
                
                _ => new ValidationResult("El atributo PersonName solo se puede aplicar a propiedades de tipo string")
            };
        }
    }
}