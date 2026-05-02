using Mercadito.Users.Api.Domain.Shared.Validation;
using Mercadito.Users.Api.Application.Users.Models;

namespace Mercadito.Users.Api.Application.Users.Validation
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
