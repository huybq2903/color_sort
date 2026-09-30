/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace Falcon.Helpers.Devkit
{
    public interface IFFile
    {
        bool Exists();
        Task SaveAsync(Stream data, int bufferSize = 4096, CancellationToken token = default);
        Task AppendAsync(Stream data, int bufferSize = 4096, CancellationToken token = default);
        Task<Stream> LoadAsync(CancellationToken token = default);

        void Delete();
        DateTime CreatedTimeUtc();
        DateTime LastWriteTimeUtc();
    }

    public static class IFFileExtensions
    {
        // Async helpers
        public static Task SaveAsync(this IFFile file, byte[] data, int bufferSize = 4096, CancellationToken token = default)
        {
            // MemoryStream should not be disposed by SaveAsync caller; SaveAsync will read and return.
            return file.SaveAsync(new MemoryStream(data, writable: false), bufferSize, token);
        }

        public static Task SaveAsync(this IFFile file, string data, Encoding encoding = null, int bufferSize = 4096, CancellationToken token = default)
        {
            encoding ??= Encoding.UTF8;
            return SaveAsync(file, encoding.GetBytes(data ?? string.Empty), bufferSize, token);
        }

        public static Task SaveAsync(this IFFile file, object data, Encoding encoding = null, int bufferSize = 4096, CancellationToken token = default)
        {
            // assume ToJson extension exists
            return SaveAsync(file, data.ToJson(), encoding, bufferSize, token);
        }

        // NOTE: AppendAsync(byte[]) should call file.AppendAsync, not SaveAsync
        public static Task AppendAsync(this IFFile file, byte[] data, int bufferSize = 4096, CancellationToken token = default)
        {
            return file.AppendAsync(new MemoryStream(data, writable: false), bufferSize, token);
        }

        public static Task AppendAsync(this IFFile file, string data, Encoding encoding = null, int bufferSize = 4096, CancellationToken token = default)
        {
            encoding ??= Encoding.UTF8;
            return AppendAsync(file, encoding.GetBytes(data ?? string.Empty), bufferSize, token);
        }

        public static Task AppendAsync(this IFFile file, Object data, Encoding encoding = null, int bufferSize = 4096, CancellationToken token = default)
        {
            return AppendAsync(file, data.ToJson(), encoding, bufferSize, token);
        }

        public static async Task<byte[]> LoadBytesAsync(this IFFile file, CancellationToken token = default)
        {
            await using var stream = await file.LoadAsync(token).ConfigureAwait(false);
            await using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, token).ConfigureAwait(false);
            return memoryStream.ToArray();
        }

        public static async Task<string> LoadStringAsync(this IFFile file, Encoding encoding = null, CancellationToken token = default)
        {
            encoding ??= Encoding.UTF8;
            var bytes = await LoadBytesAsync(file, token).ConfigureAwait(false);
            return encoding.GetString(bytes);
        }

        public static async Task<T> LoadJsonAsync<T>(this IFFile file, Encoding encoding = null, CancellationToken token = default)
        {
            var json = await LoadStringAsync(file, encoding, token).ConfigureAwait(false);
            return json.JsonToObjStrict<T>();
        }

        //
        // Synchronous wrappers (safer): run the async on thread-pool and block there.
        // This avoids deadlocks that happen when you block the main thread and the task
        // needs to resume on the same synchronization context.
        //

        // Helper to run a Task-returning func synchronously on threadpool and unwrap exceptions
        private static void RunSync(Func<Task> taskFactory)
        {
            Task.Run(taskFactory).GetAwaiter().GetResult();
        }

        private static T RunSync<T>(Func<Task<T>> taskFactory)
        {
            return Task.Run(taskFactory).GetAwaiter().GetResult();
        }

        // sync wrappers
        public static void Save(this IFFile file, Stream data, int bufferSize = 4096)
        {
            RunSync(() => file.SaveAsync(data, bufferSize));
        }

        public static void Save(this IFFile file, byte[] data, int bufferSize = 4096)
        {
            RunSync(() => SaveAsync(file, data, bufferSize));
        }

        public static void Save(this IFFile file, string data, Encoding encoding = null, int bufferSize = 4096)
        {
            RunSync(() => SaveAsync(file, data, encoding, bufferSize));
        }

        public static void Save(this IFFile file, object data, Encoding encoding = null, int bufferSize = 4096)
        {
            RunSync(() => SaveAsync(file, data, encoding, bufferSize));
        }

        public static void Append(this IFFile file, Stream data, int bufferSize = 4096)
        {
            RunSync(() => file.AppendAsync(data, bufferSize));
        }

        public static void Append(this IFFile file, byte[] data, int bufferSize = 4096)
        {
            RunSync(() => AppendAsync(file, data, bufferSize));
        }

        public static void Append(this IFFile file, string data, Encoding encoding = null, int bufferSize = 4096)
        {
            RunSync(() => AppendAsync(file, data, encoding, bufferSize));
        }

        public static void Append(this IFFile file, Object data, Encoding encoding = null, int bufferSize = 4096)
        {
            RunSync(() => AppendAsync(file, data, encoding, bufferSize));
        }

        public static Stream Load(this IFFile file)
        {
            // Caller gets the Stream and is responsible for disposing it.
            return RunSync(() => file.LoadAsync());
        }

        public static byte[] LoadBytes(this IFFile file)
        {
            return RunSync(() => LoadBytesAsync(file));
        }

        public static string LoadString(this IFFile file, Encoding encoding = null)
        {
            return RunSync(() => LoadStringAsync(file, encoding));
        }

        public static T LoadJson<T>(this IFFile file, Encoding encoding = null)
        {
            return RunSync(() => LoadJsonAsync<T>(file, encoding));
        }
    }
}