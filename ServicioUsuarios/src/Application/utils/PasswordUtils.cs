using System;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Application.Utils
{
    public static class PasswordUtils
    {
        private readonly static int SaltSize = 16; 
        private readonly static int HashSize = 32; 
        private readonly static int Iterations = 4; 
        private readonly static int DegreeOfParallelism = 2;
        // 1024 * 256 KB = 256 MB (Excelente configuración para Argon2id)
        private readonly static int MemorySize = 1024 * 256; 

        // Conjunto de caracteres seguro y legible (sin caracteres confusos si se desea, o completo)
        private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_-+=<>?";

        public static string HashPassword(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            byte[] hash = HashPassword(password, salt);
            var combinedBytes = new byte[salt.Length + hash.Length];
            Array.Copy(salt, 0, combinedBytes, 0, salt.Length);
            Array.Copy(hash, 0, combinedBytes, salt.Length, hash.Length);
            return Convert.ToBase64String(combinedBytes);
        }

        private static byte[] HashPassword(string password, byte[] salt)
        {
            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password)))
            {
                argon2.Salt = salt;
                argon2.DegreeOfParallelism = DegreeOfParallelism;
                argon2.MemorySize = MemorySize;
                argon2.Iterations = Iterations;
                return argon2.GetBytes(HashSize);
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                byte[] combinedBytes = Convert.FromBase64String(hashedPassword);
                if (combinedBytes.Length != SaltSize + HashSize) return false;

                byte[] salt = new byte[SaltSize];
                byte[] hash = new byte[HashSize];
                Array.Copy(combinedBytes, 0, salt, 0, SaltSize);
                Array.Copy(combinedBytes, SaltSize, hash, 0, HashSize);

                byte[] newHash = HashPassword(password, salt);

                return CryptographicOperations.FixedTimeEquals(hash, newHash);
            }
            catch
            {
                return false; // Previene fallos por strings Base64 malformados
            }
        }

        public static (string Password, string Hash) GenerateSecurePassword(int length = 15)
        {
            if (length < 8) throw new ArgumentException("La contraseña debe tener al menos 8 caracteres por seguridad.");

            StringBuilder passwordBuilder = new StringBuilder(length);
            
            // Usamos el método moderno y eficiente de .NET para generar enteros criptográficos
            for (int i = 0; i < length; i++)
            {
                int randomIndex = RandomNumberGenerator.GetInt32(0, AllowedChars.Length);
                passwordBuilder.Append(AllowedChars[randomIndex]);
            }

            string securePassword = passwordBuilder.ToString();
            string passwordHash = HashPassword(securePassword);

            return (securePassword, passwordHash);
        }

        public static (string Token, string Hash) GeneratePasswordResetToken(int lengthInBytes = 32)
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(lengthInBytes);
            var token = Base64UrlEncode(tokenBytes);
            var hash = HashToken(token);

            return (token, hash);
        }

        public static string HashToken(string token)
        {
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = SHA256.HashData(tokenBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private static string Base64UrlEncode(byte[] value)
        {
            return Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}