using Konscious.Security.Cryptography;
using System.Text;

namespace PasswordManager
{
    public static class PasswordKeyDerivation
    {
        public static byte[] DeriveKey(
            string password,
            byte[] encryptionSalt)
        {
            byte[] passwordBytes =
                Encoding.UTF8.GetBytes(password);

            Argon2id argon2 =
                new Konscious.Security.Cryptography.Argon2id(
                    passwordBytes)
                {
                    Salt = encryptionSalt,
                    DegreeOfParallelism = 2,
                    Iterations = 3,
                    MemorySize = 65536
                };

            return argon2.GetBytes(32);
        }
    }
}