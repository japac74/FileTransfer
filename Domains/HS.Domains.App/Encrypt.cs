using System.Security.Cryptography;

namespace HS.Domains.App
{
    public class Encrypt
    {
        /// <summary>
        /// Computes the MD5 hash of a byte array
        /// </summary>
        /// <param name="bytes">byte array</param>
        /// <returns>MD5 hash as a string</returns>
        public static string ComputeMD5(byte[] bytes)
        {
            byte[] hashBytes = MD5.HashData(bytes);
            return Convert.ToHexString(hashBytes);
        }


        /// <summary>
        /// Computes the SHA-256 hash of a file
        /// </summary>
        /// <param name="filePath">path to the file</param>
        /// <returns>SHA-256 hash as a string</returns>
        public static string ComputeSHA256(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            byte[] hashBytes = SHA256.HashData(stream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        /// <summary>
        /// Computes the SHA-1 hash of a file
        /// </summary>
        /// <param name="filePath">path to the file</param>
        /// <returns>SHA-1 hash as a string</returns>
        public static string ComputeSHA1(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            byte[] hashBytes = SHA1.HashData(stream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
