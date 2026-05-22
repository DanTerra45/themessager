using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Application.utils
{
    public static class PasswordUtils
    {
        private readonly static int SaltSize = 16; // Size of the salt in bytes
        private readonly static int HashSize = 32; // Size of the hash in bytes
        private readonly static int Iterations = 4; // Number of iterations for the hashing algorithm
        private readonly static int DegreeOfParallelism = 2;
        private readonly static int MemorySize = 1024 * 256; // Memory size in bytes (1 MB)

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
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = DegreeOfParallelism,
                MemorySize = MemorySize,
                Iterations = Iterations
            };
            return argon2.GetBytes(HashSize);
        }
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            byte[] combinedBytes = Convert.FromBase64String(hashedPassword);

            byte[] salt = new byte[SaltSize];
            byte[] hash = new byte[HashSize];
            Array.Copy(combinedBytes, 0, salt, 0, SaltSize);
            Array.Copy(combinedBytes, SaltSize, hash, 0, HashSize);

            byte[] newHash = HashPassword(password, salt);

            return CryptographicOperations.FixedTimeEquals(hash, newHash);
        }
        public static (string Password, string Hash) GenerateSecurePassword(int length = 15)
        {
            byte[] randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            string securePassword = Convert.ToBase64String(randomBytes).Substring(0, length);
            string passwordHash = HashPassword(securePassword);
            Console.WriteLine($"Password: {securePassword} | Hash: {passwordHash}");
            return (securePassword, passwordHash);
        }
    }
}