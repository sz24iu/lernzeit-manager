using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Noter.Application.HashingUnits
{
    public static class PasswordHasher
    {
        private const int saltSize = 16;
        private const int keySize = 32;
        private const int iterations = 10000;
        private static char delimiter = ';';

        private static readonly HashAlgorithmName hashAlgorithmName = HashAlgorithmName.SHA256;

        public static string Secure(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(saltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithmName, keySize);

            return string.Join(delimiter, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        public static bool Validete(string hashPassword, string inputPassword)
        {
            var hashComponents = hashPassword.Split(delimiter);
            var salt = Convert.FromBase64String(hashComponents[0]);
            var hash = Convert.FromBase64String(hashComponents[1]);

            var hashInput = Rfc2898DeriveBytes.Pbkdf2(inputPassword, salt, iterations, hashAlgorithmName, keySize);

            return CryptographicOperations.FixedTimeEquals(hash, hashInput);
        }
    }
}
