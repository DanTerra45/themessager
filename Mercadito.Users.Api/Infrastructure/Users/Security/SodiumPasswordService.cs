using Mercadito.Users.Api.Application.Users.Ports.Output;
using Sodium;

namespace Mercadito.Users.Api.Infrastructure.Users.Security
{
    public sealed class SodiumPasswordService : IPasswordHasher, IPasswordVerifier
    {
        public string Hash(string plainTextPassword)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword))
            {
                throw new ArgumentException("La contraseña no puede estar vacía.", nameof(plainTextPassword));
            }

            return PasswordHash.ArgonHashString(plainTextPassword, PasswordHash.StrengthArgon.Interactive).TrimEnd('\0');
        }

        public bool Verify(string plainTextPassword, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword) || string.IsNullOrWhiteSpace(passwordHash))
            {
                return false;
            }

            return PasswordHash.ArgonHashStringVerify(passwordHash, plainTextPassword);
        }
    }
}
