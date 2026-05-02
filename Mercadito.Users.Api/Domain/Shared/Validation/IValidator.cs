namespace Mercadito.Users.Api.Domain.Shared.Validation
{
    public interface IValidator<TInput, TOutput>
    {
        Result<TOutput> Validate(TInput input);
    }
}
