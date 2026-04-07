using Mercadito.src.shared.domain.validator;
using Mercadito.src.users.application.models;

namespace Mercadito.src.users.application.validation
{
    public interface ICreateUserValidator : IValidator<CreateUserDto, CreateUserDto>
    {
    }

    public interface IResetUserPasswordValidator : IValidator<ResetUserPasswordDto, ResetUserPasswordDto>
    {
    }

    public interface ILoginUserValidator : IValidator<LoginUserCommand, LoginUserCommand>
    {
    }

    public interface IRequestPasswordResetValidator : IValidator<RequestPasswordResetDto, RequestPasswordResetDto>
    {
    }

    public interface ICompletePasswordResetValidator : IValidator<CompletePasswordResetDto, CompletePasswordResetDto>
    {
    }
}
