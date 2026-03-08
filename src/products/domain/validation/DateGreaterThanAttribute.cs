using System;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.products.domain.validation
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateGreaterThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            var currentValue = (DateTime)value;
            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);

            if (property == null)
            {
                return new ValidationResult($"Property with name {_comparisonProperty} not found");
            }

            var comparisonValue = property.GetValue(validationContext.ObjectInstance);

            if (comparisonValue != null && currentValue <= (DateTime)comparisonValue)
            {
                if (!string.IsNullOrEmpty(ErrorMessage))
                {
                    return new ValidationResult(ErrorMessage);
                }
                return new ValidationResult($"Date must be greater than {_comparisonProperty}");
            }

            return ValidationResult.Success;
        }
    }
}
