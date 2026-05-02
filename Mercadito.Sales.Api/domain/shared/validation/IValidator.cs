namespace Mercadito.src.domain.shared.validation
{
    public interface IValidator<TInput, TOutput>
    {
        Result<TOutput> Validate(TInput input);
    }
}
