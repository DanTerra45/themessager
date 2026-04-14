using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Mercadito.src.application.users.validation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class PositiveAttribute : ValidationAttribute, IClientModelValidator
    {
        [Required(ErrorMessage = "El {0} es obligatorio")]
        public required string FieldName { get; set; }

        private string BuildRequiredMessage()
        {
            return $"El {FieldName} es obligatorio";
        }

        private string BuildPositiveMessage(bool allowZero)
        {
            if (allowZero)
            {
                return $"El {FieldName} debe ser un número entero positivo";
            }

            return $"El {FieldName} debe ser un número decimal positivo";
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var modelType = context.ModelMetadata.ModelType;
            var underlyingType = Nullable.GetUnderlyingType(modelType);
            if (underlyingType != null)
            {
                modelType = underlyingType;
            }

            var allowZero = modelType == typeof(int);
            var allowZeroFlag = "false";
            if (allowZero)
            {
                allowZeroFlag = "true";
            }

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-positive", BuildPositiveMessage(allowZero));
            MergeAttribute(context.Attributes, "data-val-positive-required", BuildRequiredMessage());
            MergeAttribute(context.Attributes, "data-val-positive-allowzero", allowZeroFlag);
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            return value switch
            {
                null => new ValidationResult(BuildRequiredMessage()),
                int i when i < 0 => new ValidationResult($"El {FieldName} debe ser un número entero positivo"),
                int => ValidationResult.Success,
                decimal d when d <= 0 => new ValidationResult($"El {FieldName} debe ser un número decimal positivo"),
                decimal => ValidationResult.Success,
                _ => new ValidationResult("El atributo Positive solo se puede aplicar a propiedades de tipo int o decimal")
            };
        }

        private static bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key))
            {
                return false;
            }

            attributes.Add(key, value);
            return true;
        }
    }
}
