using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Mercadito.src.users.application.use_cases
{
    internal static class PasswordResetTokenCodec
    {
        internal static string CreatePlainToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            return WebEncoders.Base64UrlEncode(tokenBytes);
        }

        internal static string HashToken(string token)
        {
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = SHA256.HashData(tokenBytes);
            return Convert.ToHexString(hashBytes);
        }
    }
}
