using Mercadito.Users.Api.Application.Users.Models;
using Mercadito.Users.Api.Application.Users.Ports.Input;
using Mercadito.Users.Api.Application.Users.Ports.Output;
using Mercadito.Users.Api.Application.Users.Validation;
using Mercadito.Users.Api.Domain.Shared;

namespace Mercadito.Users.Api.Application.Users.UseCases
{
    public sealed class AuthenticateUserUseCase(
        IUserRepository userRepository,
        IPasswordVerifier passwordVerifier,
        ILoginUserValidator validator) : IAuthenticateUserUseCase
    {
        public async Task<Result<AuthenticatedUser>> ExecuteAsync(LoginUserCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = validator.Validate(command);
            if (validationResult.IsFailure)
            {
                return Result.Failure<AuthenticatedUser>(validationResult.Errors);
            }

            var normalizedCommand = validationResult.Value;
            var user = await userRepository.GetActiveByUsernameAsync(normalizedCommand.Username, cancellationToken);
            if (user == null)
            {
                return Result.Failure<AuthenticatedUser>("Usuario o contraseña inválidos.");
            }

            if (!passwordVerifier.Verify(normalizedCommand.Password, user.PasswordHash))
            {
                return Result.Failure<AuthenticatedUser>("Usuario o contraseña inválidos.");
            }

            var lastLogin = DateTime.UtcNow;
            await userRepository.UpdateLastLoginAsync(user.Id, lastLogin, cancellationToken);

            return Result.Success(new AuthenticatedUser
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                EmployeeId = user.EmployeeId,
                MustChangePassword = user.MustChangePassword,
                LastLogin = lastLogin
            });
        }
    }
}
