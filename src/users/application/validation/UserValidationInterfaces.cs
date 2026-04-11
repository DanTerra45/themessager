using Mercadito.src.shared.domain.validation;
using Mercadito.src.users.application.models;

namespace Mercadito.src.users.application.validation
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
