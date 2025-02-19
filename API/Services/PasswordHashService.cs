using System.Security.Cryptography;

namespace API.Services
{
    public class PasswordHashService
    {
        private const int SaltSize = 16; // 128 bits
        private const int HashSize = 32; // 256 bits
        private const int Iterations = 10000;

        public string HashPassword(string password)
        {
            // Generar un salt aleatorio
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Crear el hash
            byte[] hash = GetHash(password, salt);

            // Combinar salt y hash
            byte[] hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

            // Convertir a string para almacenamiento
            return Convert.ToBase64String(hashBytes);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            // Convertir el hash almacenado de vuelta a bytes
            byte[] hashBytes = Convert.FromBase64String(hashedPassword);

            // Extraer salt
            byte[] salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            // Extraer hash original
            byte[] originalHash = new byte[HashSize];
            Array.Copy(hashBytes, SaltSize, originalHash, 0, HashSize);

            // Generar hash con la contraseña proporcionada y el salt extraído
            byte[] newHash = GetHash(password, salt);

            // Comparar los hashes
            return CompareBytes(originalHash, newHash);
        }

        private byte[] GetHash(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(HashSize);
            }
        }

        private bool CompareBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}
