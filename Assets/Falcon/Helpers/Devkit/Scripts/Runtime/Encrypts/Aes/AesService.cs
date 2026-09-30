/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.IO;
using System.Security.Cryptography;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public static class AesService
    {
        // Define default AES parameters as constants for consistency and easy modification
        private const CipherMode kDefaultCipherMode = CipherMode.CBC;
        private const PaddingMode kDefaultPaddingMode = PaddingMode.PKCS7;

        /// <summary>
        ///     Generates a new random AES key.
        /// </summary>
        /// <returns>A byte array representing the AES key.</returns>
        public static byte[] GenerateAesKey()
        {
            using var aesAlg = Aes.Create();
            aesAlg.Mode = kDefaultCipherMode; // Set mode for consistency, though not strictly needed for key generation
            aesAlg.Padding = kDefaultPaddingMode; // Set padding for consistency
            aesAlg.GenerateKey();
            return aesAlg.Key;
        }

        /// <summary>
        ///     Generates a new random AES key and converts it to a Base64 string.
        /// </summary>
        /// <returns>A Base64 string representation of the AES key.</returns>
        public static string GenerateAesKeyStr()
        {
            return Convert.ToBase64String(GenerateAesKey());
        }

        /// <summary>
        ///     Encrypts the provided content using AES (CBC mode, PKCS7 padding).
        ///     A new, random IV is generated for each encryption.
        /// </summary>
        /// <param name="key">The AES encryption key.</param>
        /// <param name="content">The plaintext content to encrypt.</param>
        /// <returns>An AesEncrypted object containing the ciphertext and the IV.</returns>
        /// <exception cref="ArgumentNullException">Thrown if key or content is null.</exception>
        /// <exception cref="CryptographicException">Thrown if an error occurs during encryption.</exception>
        public static AesEncrypted Encrypt(byte[] key, Stream content)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (content == null) throw new ArgumentNullException(nameof(content));

            using var aesAlg = Aes.Create();

            aesAlg.Key = key;
            aesAlg.Mode = kDefaultCipherMode;
            aesAlg.Padding = kDefaultPaddingMode;
            aesAlg.GenerateIV(); // Crucial: Generate a new IV for each encryption

            var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            try
            {
                // using var memoryStream = new MemoryStream();
                // using var cryptoStream = new CryptoStream(content, encryptor, CryptoStreamMode.Write);
                
                // cryptoStream.Write(content, 0, content.Length);
                // cryptoStream.FlushFinalBlock(); // Ensures all buffered data and padding is written
                //
                // var encryptedData = memoryStream.ToArray();
                return new AesEncrypted(new CryptoStream(content, encryptor, CryptoStreamMode.Read), aesAlg.IV);
            }
            catch (CryptographicException ex)
            {
                // Log or handle the specific cryptographic error
                throw new CryptographicException("Encryption failed.", ex);
            }
        }

        /// <summary>
        ///     Decrypts the provided encrypted data using AES (CBC mode, PKCS7 padding).
        /// </summary>
        /// <param name="key">The AES decryption key.</param>
        /// <param name="encrypted">An AesEncrypted object containing the ciphertext and the IV used during encryption.</param>
        /// <returns>The decrypted plaintext content as a byte array.</returns>
        /// <exception cref="ArgumentNullException">Thrown if key or encrypted is null.</exception>
        /// <exception cref="CryptographicException">
        ///     Thrown if an error occurs during decryption (e.g., corrupted data, incorrect
        ///     key/IV).
        /// </exception>
        public static Stream Decrypt(byte[] key, AesEncrypted encrypted)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (encrypted == null) throw new ArgumentNullException(nameof(encrypted));

            using var aesAlg = Aes.Create();
            aesAlg.Key = key;
            aesAlg.IV = encrypted.Iv; // Crucial: Use the same IV that was used for encryption
            aesAlg.Mode = kDefaultCipherMode;
            aesAlg.Padding = kDefaultPaddingMode;

            var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            try
            {
                return new CryptoStream(encrypted.Data, decryptor, CryptoStreamMode.Read);
            }
            catch (CryptographicException ex)
            {
                // Log or handle the specific cryptographic error
                throw new CryptographicException("Decryption failed. Data may be corrupted or key/IV is incorrect.",
                    ex);
            }
        }

        // /// <summary>
        // ///     Reads the entire content of a stream into a byte array.
        // /// </summary>
        // /// <param name="input">The stream to read from.</param>
        // /// <returns>A byte array containing the full content of the stream.</returns>
        // /// <exception cref="ArgumentNullException">Thrown if the input stream is null.</exception>
        // private static byte[] ReadFully(this Stream input)
        // {
        //     if (input == null) throw new ArgumentNullException(nameof(input));
        //
        //     using var ms = new MemoryStream();
        //     var buffer = new byte[kReadBufferSize];
        //     int read;
        //     while ((read = input.Read(buffer, 0, buffer.Length)) > 0) ms.Write(buffer, 0, read);
        //     return ms.ToArray();
        // }
    }
}