using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager
{
    public static class PasswordHasher
    {
        public static byte[] CreateSalt()
        {
            return RandomNumberGenerator.GetBytes(16);
        }

        public static byte[] HashPassword(
            string password,
            byte[] salt)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            Argon2id argon2 = new Argon2id(passwordBytes)
            {
                Salt = salt,
                DegreeOfParallelism = 2,
                Iterations = 3,
                MemorySize = 65536
            };

            return argon2.GetBytes(32);
        }

        public static bool VerifyPassword(
            string password,
            byte[] salt,
            byte[] expectedHash)
        {
            byte[] actualHash = HashPassword(password, salt);

            return CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash);
        }
    }
}