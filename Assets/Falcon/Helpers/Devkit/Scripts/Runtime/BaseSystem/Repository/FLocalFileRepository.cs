/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    [NoLazy]
    public class FLocalFileRepository : IMySingleton
    {
        private readonly string _persistentDataPath;
        private readonly string _tempPath; // Đường dẫn đến thư mục tệp tạm thời

        public FLocalFileRepository()
        {
            // 1. Xác định đường dẫn chính cho dữ liệu (Persistent Data hoặc Streaming Assets)
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            // Trên macOS (Editor hoặc Standalone), thường dùng StreamingAssets cho các tệp đã đóng gói.
            // Cẩn thận: StreamingAssets là chỉ đọc trong các bản build.
            _persistentDataPath = Application.streamingAssetsPath;
#else
            // Trên các nền tảng khác, persistentDataPath là đường dẫn đọc/ghi an toàn.
            _persistentDataPath = Application.persistentDataPath;
#endif
            // Xử lý trường hợp null hoặc rỗng, mặc dù Application.persistentDataPath/streamingAssetsPath hiếm khi null.
            if (string.IsNullOrEmpty(_persistentDataPath))
                throw new IOException(
                    "FFileRepository: _persistentDataPath is null or empty. This might indicate an issue with Unity paths.");
            // Có thể dùng một fallback hoặc ném ngoại lệ tùy vào logic của bạn.

            // 2. Lấy đường dẫn đến thư mục tệp tạm thời
            _tempPath = Application.temporaryCachePath;
            if (string.IsNullOrEmpty(_tempPath))
                BaseSystemLogger.Instance.Error(
                    "FFileRepository: _tempPath is null or empty. This might indicate an issue with Unity temporary paths.");
        }

        public string GetFolder(string folder)
        {
            if (!folder.StartsWith(_persistentDataPath)) folder = Path.Combine(_persistentDataPath, folder);
            return folder;
        }
        
        public FLocalFile GetFile(string fileName)
        {
            if (!fileName.StartsWith(_persistentDataPath)) fileName = Path.Combine(_persistentDataPath, fileName);
            return new FLocalFile(fileName);
        }

        public FLocalFile GetTempFile(string fileName)
        {
            var folder = _tempPath;
            if (string.IsNullOrEmpty(folder)) folder = _persistentDataPath;
            if (!fileName.StartsWith(folder)) fileName = Path.Combine(folder, fileName);
            return new FLocalFile(fileName);
        }

        public IEnumerable<FLocalFile> ListFilesUsingFolderPrefix(string folderPrefix)
        {
            if (!folderPrefix.StartsWith(_persistentDataPath)) folderPrefix = Path.Combine(_persistentDataPath, folderPrefix);
            
            if (!Directory.Exists(folderPrefix)) return Enumerable.Empty<FLocalFile>();

            return new DirectoryInfo(Path.Combine(_persistentDataPath, folderPrefix))
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Select(info => new FLocalFile(info.FullName));
        }

        public IEnumerable<FLocalFile> ListFilesAtFolderExact(string folderPrefix)
        {
            if (!folderPrefix.StartsWith(_persistentDataPath)) folderPrefix = Path.Combine(_persistentDataPath, folderPrefix);
            
            if (!Directory.Exists(folderPrefix)) return Enumerable.Empty<FLocalFile>();
            return new DirectoryInfo(Path.Combine(_persistentDataPath, folderPrefix))
                .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                .Select(info => new FLocalFile(info.FullName));
        }

        public IEnumerable<FLocalFile> ListTempFilesUsingFolderPrefix(string folder)
        {
            if (!folder.StartsWith(_persistentDataPath))
                folder = Path.Combine(_persistentDataPath, folder);
            
            if (!Directory.Exists(folder)) return Enumerable.Empty<FLocalFile>();
            return _tempPath == null
                ? Array.Empty<FLocalFile>().AsEnumerable()
                : new DirectoryInfo(Path.Combine(_tempPath, folder))
                    .EnumerateFiles("*", SearchOption.AllDirectories)
                    .Select(info => new FLocalFile(info.FullName));
        }

        public IEnumerable<FLocalFile> ListTempFilesAtFolderExact(string folder)
        {
            if (!folder.StartsWith(_persistentDataPath))
                folder = Path.Combine(_persistentDataPath, folder);
            
            if (!Directory.Exists(folder)) return Enumerable.Empty<FLocalFile>();
            return _tempPath == null
                ? Array.Empty<FLocalFile>().AsEnumerable()
                : new DirectoryInfo(Path.Combine(_tempPath, folder))
                    .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                    .Select(info => new FLocalFile(info.FullName));
        }
    }
}