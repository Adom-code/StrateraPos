using System;
using System.Security.Cryptography;
using System.Text;

namespace StrateraPos.Services
{
    public class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32; // 256 bits
        private const int Iterations = 10000;

        /// <summary>
        /// Creates a password hash and salt
        /// </summary>
        public static (string hash, string salt) HashPassword(string password)
        {
            // Generate a random salt
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] saltBytes = new byte[SaltSize];
                rng.GetBytes(saltBytes);
                string salt = Convert.ToBase64String(saltBytes);

                // Hash the password with the salt
                string hash = HashPasswordWithSalt(password, salt);

                return (hash, salt);
            }
        }

        /// <summary>
        /// Verifies a password against a hash and salt
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            string hashToVerify = HashPasswordWithSalt(password, storedSalt);
            return hashToVerify == storedHash;
        }

        /// <summary>
        /// Hashes a password with a given salt
        /// </summary>
        private static string HashPasswordWithSalt(string password, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);

            // Use the modern static Pbkdf2 method (recommended by .NET)
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize
            );

            return Convert.ToBase64String(hash);
        }
    }
}