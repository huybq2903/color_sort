/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-03
     */

namespace Falcon.Helpers.Security
{
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    
    public static class Encryption
    {
        private static readonly Dictionary<string, string> kCacheMD5 = new();
        
        /// <summary>
        /// Encrypt string with MD5
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns>Encrypted string</returns>
        public static string MD5(string inputString)
        {
            if (kCacheMD5.TryGetValue(inputString, out var md5))
                return md5;
            
            MD5CryptoServiceProvider x  = new MD5CryptoServiceProvider();
            byte[]                   bs = System.Text.Encoding.UTF8.GetBytes(inputString);
            bs = x.ComputeHash(bs);
            StringBuilder s = new StringBuilder();
            foreach (byte b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }

            kCacheMD5[inputString] = s.ToString();
            return kCacheMD5[inputString];
        }

        /// <summary>
        /// Hash string with SHA1 algorithm
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns>Hash SHA1</returns>
        public static string SHA1Hash(string inputString)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[]        hashBytes     = sha1.ComputeHash(Encoding.UTF8.GetBytes(inputString));
                StringBuilder stringBuilder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    stringBuilder.Append(b.ToString("x2"));
                }

                return stringBuilder.ToString();
            }
        }
        
        /// <summary>
        /// MD5 File
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string MD5File(string filename)
        {
            using (MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = System.IO.File.OpenRead(filename))
                {
                    return Encoding.Default.GetString(md5.ComputeHash(stream));
                }
            }
        }
    }
}
