using Domain.Common;
namespace Domain.Validation
{
    public interface IValidator<in T> where T : class
    {
        Result<bool> Validate(T instance);
    }
}