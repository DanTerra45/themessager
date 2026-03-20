using System.ComponentModel.DataAnnotations;

namespace Mercadito
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PersonNameAttribute : ValidationAttribute
    {
        [Required(ErrorMessage = "El {0} es obligatorio")]
        public required string FieldName { get; set; }

        private bool isValidRegex(object? value)
        {
            if (value is not string s)
                return false;
            var regex = @"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s'-]+$";
            return System.Text.RegularExpressions.Regex.IsMatch(s, regex);
        }


        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            return value switch
            {
                null => new ValidationResult($"El {FieldName} es obligatorio"),
                string s when string.IsNullOrWhiteSpace(s) => new ValidationResult($"El {FieldName} es obligatorio"),
                string s when s.Length > 20 => new ValidationResult($"El {FieldName} no puede exceder 20 caracteres"),
                string s when !isValidRegex(s) => new ValidationResult($"El {FieldName} contiene caracteres no permitidos"),
                _ => new ValidationResult("El atributo PersonName solo se puede aplicar a propiedades de tipo string")
            };
        }
    }
}
