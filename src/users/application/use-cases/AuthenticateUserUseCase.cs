using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.users.application.validation;
using Shared.Domain;

namespace Mercadito.src.users.application.use_cases
{
    public sealed class AuthenticateUserUseCase : IAuthenticateUserUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordVerifier _passwordVerifier;
        private readonly ILoginUserValidator _validator;

        public AuthenticateUserUseCase(
            IUserRepository userRepository,
            IPasswordVerifier passwordVerifier,
            ILoginUserValidator validator)
        {
            _userRepository = userRepository;
            _passwordVerifier = passwordVerifier;
            _validator = validator;
        }

        public async Task<Result<AuthenticatedUser>> ExecuteAsync(LoginUserCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _validator.Validate(command);
            if (validationResult.IsFailure)
            {
                return Result<AuthenticatedUser>.Failure(validationResult.Errors);
            }

            var normalizedCommand = validationResult.Value;
            var user = await _userRepository.GetActiveByUsernameAsync(normalizedCommand.Username, cancellationToken);
            if (user == null)
            {
                return Result<AuthenticatedUser>.Failure("Usuario o contraseña inválidos.");
            }

            if (!_passwordVerifier.Verify(normalizedCommand.Password, user.PasswordHash))
            {
                return Result<AuthenticatedUser>.Failure("Usuario o contraseña inválidos.");
            }

            var lastLogin = DateTime.UtcNow;
            await _userRepository.UpdateLastLoginAsync(user.Id, lastLogin, cancellationToken);

            return Result<AuthenticatedUser>.Success(new AuthenticatedUser
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                EmployeeId = user.EmployeeId,
                LastLogin = lastLogin
            });
        }
    }
}
