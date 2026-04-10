namespace Mercadito.src.shared.domain.validation
{
    public interface IValidator<TInput, TOutput>
    {
        Result<TOutput> Validate(TInput input);
    }
}
