using Mercadito.src.users.application.models;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Shared.Domain;

namespace Mercadito.src.users.application.use_cases
{
    public sealed class ValidatePasswordResetTokenUseCase : IValidatePasswordResetTokenUseCase
    {
        private readonly IUserRepository _userRepository;

        public ValidatePasswordResetTokenUseCase(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<PasswordResetTokenInfo>> ExecuteAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Result<PasswordResetTokenInfo>.Failure("El enlace de restablecimiento es inválido.");
            }

            var tokenHash = PasswordResetTokenCodec.HashToken(token.Trim());
            var tokenInfo = await _userRepository.GetValidPasswordResetTokenAsync(tokenHash, DateTime.UtcNow, cancellationToken);
            if (tokenInfo == null)
            {
                return Result<PasswordResetTokenInfo>.Failure("El enlace de restablecimiento es inválido o ya venció.");
            }

            return Result<PasswordResetTokenInfo>.Success(tokenInfo);
        }
    }
}
