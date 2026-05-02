using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Mercadito.Users.Api.Application.Users.Validation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class CIAttribute : ValidationAttribute, IClientModelValidator
    {
        private const long MinimumCiValue = 1000000L;
        private const long MaximumCiValue = 99999999L;

        [Required(ErrorMessage = "El {0} es obligatorio")]
        public required string FieldName { get; set; }

        private static bool IsValidLength(long value)
        {
            return value >= MinimumCiValue && value <= MaximumCiValue;
        }

        private string BuildRequiredMessage()
        {
            return $"El {FieldName} es obligatorio";
        }

        private string BuildLengthMessage()
        {
            return $"El {FieldName} debe tener entre 7 y 8 dígitos";
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-ci", BuildLengthMessage());
            MergeAttribute(context.Attributes, "data-val-ci-required", BuildRequiredMessage());
            MergeAttribute(context.Attributes, "data-val-ci-min", MinimumCiValue.ToString(CultureInfo.InvariantCulture));
            MergeAttribute(context.Attributes, "data-val-ci-max", MaximumCiValue.ToString(CultureInfo.InvariantCulture));
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            return value switch
            {
                null => new ValidationResult(BuildRequiredMessage()),
                long l when !IsValidLength(l) => new ValidationResult(BuildLengthMessage()),
                long => ValidationResult.Success,
                _ => new ValidationResult("El atributo CI solo se puede aplicar a propiedades de tipo long")
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
