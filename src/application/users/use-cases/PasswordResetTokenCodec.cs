using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Mercadito.src.application.users.usecases
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
