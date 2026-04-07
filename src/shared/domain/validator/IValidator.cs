namespace Mercadito.src.shared.domain.validator
{
    public interface IValidator<TInput, TOutput>
    {
        Result<TOutput> Validate(TInput input);
    }
}