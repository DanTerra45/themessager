using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.users.application.validation;
using Shared.Domain;

namespace Mercadito.src.users.application.use_cases
{
    public sealed class CompletePasswordResetUseCase : ICompletePasswordResetUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICompletePasswordResetValidator _validator;

        public CompletePasswordResetUseCase(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            ICompletePasswordResetValidator validator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _validator = validator;
        }

        public async Task<Result<bool>> ExecuteAsync(CompletePasswordResetDto request, CancellationToken cancellationToken = default)
        {
            var validationResult = _validator.Validate(request);
            if (validationResult.IsFailure)
            {
                return Result<bool>.Failure(validationResult.Errors);
            }

            var normalized = validationResult.Value;
            var tokenHash = PasswordResetTokenCodec.HashToken(normalized.Token);
            var passwordHash = _passwordHasher.Hash(normalized.Password);
            var wasUpdated = await _userRepository.ResetPasswordByTokenAsync(tokenHash, passwordHash, DateTime.UtcNow, cancellationToken);

            if (!wasUpdated)
            {
                return Result<bool>.Failure("El enlace de restablecimiento es inválido o ya venció.");
            }

            return Result<bool>.Success(true);
        }
    }
}
