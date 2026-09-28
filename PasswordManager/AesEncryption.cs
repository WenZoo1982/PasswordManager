using System;
using System.Security.Cryptography;

namespace PasswordManager
{
    public static class AesEncryption
    {
        public static byte[] Encrypt(
            byte[] plaintext,
            byte[] key,
            out byte[] nonce,
            out byte[] tag)
        {
            nonce = RandomNumberGenerator.GetBytes(12);
            tag = new byte[16];

            byte[] ciphertext = new byte[plaintext.Length];

            using AesGcm aes = new AesGcm(key, 16);

            aes.Encrypt(
                nonce,
                plaintext,
                ciphertext,
                tag);

            return ciphertext;
        }

        public static byte[] Decrypt(
            byte[] ciphertext,
            byte[] key,
            byte[] nonce,
            byte[] tag)
        {
            byte[] plaintext = new byte[ciphertext.Length];

            using AesGcm aes = new AesGcm(key, 16);

            aes.Decrypt(
                nonce,
                ciphertext,
                tag,
                plaintext);

            return plaintext;
        }
    }
}