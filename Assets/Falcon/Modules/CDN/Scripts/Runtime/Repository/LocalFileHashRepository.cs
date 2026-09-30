/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;
using Falcon.Modules.Core.SaveLoad.Runtime;
using System.IO;
using System.Security.Cryptography;
using Falcon.Helpers.Devkit;

namespace Falcon.Modules.CDN
{
    public static class LocalFileHashRepository
    {
        private static readonly string DefaultEmpty = string.Empty;
        private const string kSaveLoadEntryPrefix = "F_Cdn_md5_"; 

        public static string GetFileHash(FLocalFile file)
        {
            var load = SaveLoadHandler.Load(kSaveLoadEntryPrefix + file.FilePath, DefaultEmpty);
            return string.Equals(load, DefaultEmpty, StringComparison.Ordinal) ? CalculateMD5Hash(file.FilePath) : load;
        }
        
        public static void DeleteFileHash(FLocalFile file)
        {
            SaveLoadHandler.DeleteKey(kSaveLoadEntryPrefix + file.FilePath);
        }

        public static void SaveFileHash(FLocalFile file, string hash)
        {
            SaveLoadHandler.Save(kSaveLoadEntryPrefix + file.FilePath, hash);
        }

        private static string CalculateMD5Hash(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            // Compute the hash from the file stream.
            var hash = md5.ComputeHash(stream);

            // Convert the byte array to a hexadecimal string.
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}