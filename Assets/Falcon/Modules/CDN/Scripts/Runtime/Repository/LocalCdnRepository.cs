/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Falcon.Helpers.Devkit;
using JetBrains.Annotations;

namespace Falcon.Modules.CDN
{
    public class LocalCdnRepository : IMySingleton
    {
        private const string kCdnFolder = "FalconCdn";
        private readonly FLocalFileRepository _localFileRepository;

        public LocalCdnRepository(FLocalFileRepository localFileRepository)
        {
            _localFileRepository = localFileRepository;
        }

        public FLocalFile GetFileShell(CdnItem item)
        {
            return _localFileRepository.GetFile(Path.Combine(new[] { kCdnFolder }.Concat(item.FolderSegments)
                .Concat(new[] { item.fileName })));
        }

        public bool TryGetFile(CdnItem item, out FLocalFile fFile)
        {
            fFile = GetFileShell(item);
            return fFile.Exists();
        }

        public Dictionary<CdnItem, FLocalFile> ListFilesAtFolderExact(string[] folderSegments)
        {
            return _localFileRepository
                .ListFilesAtFolderExact(Path.Combine(new[] { kCdnFolder }.Concat(folderSegments)))
                .ToDictionary(
                    file => new CdnItem(Path.GetFileName(file.FilePath), ExtractRelativeFolder(file.FilePath)));
        }

        public Dictionary<CdnItem, FLocalFile> ListFilesUsingFolderPrefix(string[] folderSegments)
        {
            var dictionary = new Dictionary<CdnItem, FLocalFile>();
            foreach (var file in
                     _localFileRepository.ListFilesUsingFolderPrefix(
                         Path.Combine(new[] { kCdnFolder }.Concat(folderSegments))))
                dictionary.Add(new CdnItem(Path.GetFileName(file.FilePath), ExtractRelativeFolder(file.FilePath)),
                    file);
            return dictionary;
        }

        [CanBeNull]
        private string[] ExtractRelativeFolder(string path)
        {
            var root = _localFileRepository.GetFolder(kCdnFolder);
            // Split by both separators for cross-platform compatibility
            return (Path.GetDirectoryName(Path.GetRelativePath(root, path)) ?? "")
                .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        }


        public static void CleanUpEmptyFolders()
        {
            DeleteEmptySubdirectories(kCdnFolder);
        }

        private static void DeleteEmptySubdirectories(string parentDirectory)
        {
            // Guard clause for a non-existent directory.
            if (!Directory.Exists(parentDirectory)) return;

            // Get all subdirectories within the current parentDirectory.
            // The SearchOption.TopDirectoryOnly is crucial here for the recursive logic.
            foreach (var directory in Directory.EnumerateDirectories(parentDirectory, "*",
                         SearchOption.TopDirectoryOnly))
                try
                {
                    // Recursively call the method on each subdirectory.
                    DeleteEmptySubdirectories(directory);

                    // After the recursive call returns, check if the subdirectory is now empty.
                    if (!Directory.EnumerateFileSystemEntries(directory).Any()) Directory.Delete(directory);
                }
                catch (IOException ex)
                {
                    // Handle cases where deletion might fail due to access rights or a non-empty directory.
                    Console.WriteLine($"Could not delete directory {directory}: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Access denied to delete directory {directory}: {ex.Message}");
                }
        }
    }
}