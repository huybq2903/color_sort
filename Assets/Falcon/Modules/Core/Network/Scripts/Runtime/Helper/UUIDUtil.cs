/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System;
using System.Security.Cryptography;
using System.Text;

namespace Falcon.Modules.Core.Network
{
    public class UUIDUtil
    {
        public static string UUID16()
        {
            string input = System.Guid.NewGuid().ToString();
            using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
            {
                byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                string shortHash = BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16);
                return shortHash;
            }

        }

        public static string UUID36()
        {
            return System.Guid.NewGuid().ToString();
        }
    }
}

