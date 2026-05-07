using System.ComponentModel.DataAnnotations;

namespace Mercadito.Frontend.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MinimumDecimalAttribute(double minimum) : ValidationAttribute
{
    private readonly decimal _minimum = Convert.ToDecimal(minimum);

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        return value switch
        {
            decimal decimalValue => decimalValue >= _minimum,
            double doubleValue => Convert.ToDecimal(doubleValue) >= _minimum,
            float floatValue => Convert.ToDecimal(floatValue) >= _minimum,
            _ => false
        };
    }
}
