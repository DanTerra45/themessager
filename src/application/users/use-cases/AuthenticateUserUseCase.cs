using Mercadito.src.application.users.models;
using Mercadito.src.application.users.ports.input;
using Mercadito.src.application.users.ports.output;
using Mercadito.src.application.users.validation;
using Mercadito.src.domain.shared;

namespace Mercadito.src.application.users.usecases
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
