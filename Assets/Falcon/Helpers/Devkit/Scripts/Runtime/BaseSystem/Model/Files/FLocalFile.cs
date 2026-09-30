/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    /// <summary>
    ///     Saves, loads and deletes all data in the game
    /// </summary>
    public class FLocalFile : IFFile
    {
        public string FilePath { get; }

        public FLocalFile(string filePath)
        {
            FilePath = filePath;
        }

        public bool Exists()
        {
            return File.Exists(FilePath);
        }

        public async Task SaveAsync(Stream data, int bufferSize = 4096, CancellationToken token = default)
        {
            // Ensure directory exists (if FilePath contains a directory)
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // If stream supports seeking, reset to start
            if (data.CanSeek)
                data.Seek(0, SeekOrigin.Begin);

            // Open file stream for asynchronous writing
            await using var fileStream = new FileStream(
                FilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize,
                FileOptions.Asynchronous
            );

            // Efficient async copy
            await data.CopyToAsync(fileStream, bufferSize, token);
        }
        
        public async Task AppendAsync(Stream data, int bufferSize = 4096, CancellationToken token = default)
        {
            // Use a using statement to ensure the streams are properly disposed of.
            await using var fileStream = new FileStream(FilePath, FileMode.Append, FileAccess.Write);
            // Set the buffer size. A buffer size of 4KB is a good default for performance.
            var buffer = new byte[bufferSize];
            int bytesRead;

            // Read from the input stream into the buffer and write to the file stream.
            while ((bytesRead = await data.ReadAsync(buffer, 0, buffer.Length, token)) > 0) 
                await fileStream.WriteAsync(buffer, 0, bytesRead, token);
        }

        public Task<Stream> LoadAsync(CancellationToken token = default)
        {
            return Task.FromResult<Stream>(File.OpenRead(FilePath));
        }

        public void Delete()
        {
            File.Delete(FilePath);
        }

        public DateTime CreatedTimeUtc()
        {
            return File.GetCreationTimeUtc(FilePath);
        }

        public DateTime LastWriteTimeUtc()
        {
            return File.GetLastWriteTimeUtc(FilePath);
        }
    }
}