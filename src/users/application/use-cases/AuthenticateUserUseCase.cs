using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.users.application.validation;
using Mercadito.src.shared.domain;

namespace Mercadito.src.users.application.usecases
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
