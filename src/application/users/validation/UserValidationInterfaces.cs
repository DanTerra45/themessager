using Mercadito.src.domain.shared.validation;
using Mercadito.src.application.users.models;

namespace Mercadito.src.application.users.validation
{
    public interface ICreateUserValidator : IValidator<CreateUserDto, CreateUserDto>
    {
    }

    public interface IAssignTemporaryPasswordValidator : IValidator<AssignTemporaryPasswordDto, AssignTemporaryPasswordDto>
    {
    }

    public interface ISendAdministrativePasswordResetLinkValidator : IValidator<SendAdministrativePasswordResetLinkDto, SendAdministrativePasswordResetLinkDto>
    {
    }

    public interface IForcePasswordChangeValidator : IValidator<ForcePasswordChangeDto, ForcePasswordChangeDto>
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
