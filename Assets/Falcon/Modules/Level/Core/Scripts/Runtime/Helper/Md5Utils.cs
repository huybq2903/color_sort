/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-11

using System;
using System.Security.Cryptography;
using System.Text;
namespace Falcon.Modules.Level.Core
{
    public static class Md5Utils
    {
        static string Normalize(string s)
        {
            if (s == null) return "";
            if (s.Length > 0 && s[0] == '\uFEFF') s = s.Substring(1);
            return s.Replace("\r\n", "\n");
        }
        
        public static string GetMd5First5Char(string input)
        {
            try
            {
                string canonical = Normalize(input);
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                    return Convert.ToBase64String(hash).Substring(0, 5);
                }
            }
            catch
            {
                return null;
            }
        }
    }


}