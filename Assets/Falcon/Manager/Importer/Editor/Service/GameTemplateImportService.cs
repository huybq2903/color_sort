/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-04-20
 */

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Manager.Shared;
using UnityEditor;
using UnityEngine;

namespace Falcon.Manager.Importer
{
    public class GameTemplateImportService
    {
        private const int    BUFFER_SIZE      = 81920;
        private const double REPAINT_INTERVAL = 0.1d;

        public async Task<bool> DownloadAndImport(GameTemplatePackage template)
        {
            string workingDir = null;

            using var cts = new CancellationTokenSource();
            var progress = new DownloadProgress();

            var lastRepaint = 0d;

            // The copy loop runs off the main thread, so the progress bar has to be pumped from
            // EditorApplication.update. This also gives the user a working Cancel button instead
            // of the download dying silently part-way through.
            void PumpProgressBar()
            {
                // Every DisplayCancelableProgressBar call repaints the window, and update fires
                // ~100x/s - throttle it so a multi-minute download does not bog down the Editor.
                var now = EditorApplication.timeSinceStartup;
                if (now - lastRepaint < REPAINT_INTERVAL) return;
                lastRepaint = now;

                if (EditorUtility.DisplayCancelableProgressBar("Game Template", progress.Describe(), progress.Normalized))
                {
                    cts.Cancel();
                }
            }

            EditorApplication.update += PumpProgressBar;

            try
            {
                var packagePath = await DownloadPackage(template.fileName, progress, cts.Token);
                workingDir = Path.GetDirectoryName(packagePath);

                // The progress bar is modal - it has to be down before the interactive
                // import dialog opens, or the dialog cannot be interacted with.
                EditorApplication.update -= PumpProgressBar;
                EditorUtility.ClearProgressBar();

                return await ImportPackage(template, packagePath, workingDir);
            }
            catch (OperationCanceledException)
            {
                DeleteWorkingDirectory(workingDir);
                Debug.LogWarning($"[GameTemplateImportService] Download canceled: {template.displayName} {template.version}");
                return false;
            }
            catch (Exception e)
            {
                DeleteWorkingDirectory(workingDir);

                // Mono can surface a cancelled request as WebException rather than
                // OperationCanceledException, so don't show a failure dialog for a user cancel.
                if (cts.IsCancellationRequested)
                {
                    Debug.LogWarning($"[GameTemplateImportService] Download canceled: {template.displayName} {template.version}");
                    return false;
                }

                Debug.LogError($"Failed to import template: {e}");
                EditorApplication.update -= PumpProgressBar;
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Import Failed", $"Failed to import template: {e.Message}", "OK");
                return false;
            }
            finally
            {
                EditorApplication.update -= PumpProgressBar;
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// AssetDatabase.ImportPackage is asynchronous - it opens the import dialog and returns
        /// immediately. The package file must stay on disk until Unity reports a terminal result,
        /// otherwise the decompress step fails with "Couldn't decompress package".
        /// </summary>
        private static Task<bool> ImportPackage(GameTemplatePackage template, string packagePath, string workingDir)
        {
            var tcs      = new TaskCompletionSource<bool>();
            var expected = Path.GetFileNameWithoutExtension(packagePath);

            // Held in locals so the -= removal uses the exact same delegate reference.
            // These are Unity's own delegate types, not Action<>.
            AssetDatabase.ImportPackageCallback       onCompleted = null;
            AssetDatabase.ImportPackageCallback       onCancelled = null;
            AssetDatabase.ImportPackageFailedCallback onFailed    = null;

            bool Matches(string packageName) =>
                string.Equals(Path.GetFileNameWithoutExtension(packageName), expected, StringComparison.OrdinalIgnoreCase);

            void Unsubscribe()
            {
                AssetDatabase.importPackageCompleted -= onCompleted;
                AssetDatabase.importPackageCancelled -= onCancelled;
                AssetDatabase.importPackageFailed    -= onFailed;
                DeleteWorkingDirectory(workingDir);
            }

            onCompleted = packageName =>
            {
                if (!Matches(packageName)) return;
                Unsubscribe();

                // Saved here rather than after the await: importing the template recompiles
                // scripts, and the domain reload that follows would drop the continuation.
                InstalledTemplateRepository.Save(template.displayName, template.version);
                tcs.TrySetResult(true);
            };

            onCancelled = packageName =>
            {
                if (!Matches(packageName)) return;
                Unsubscribe();
                Debug.LogWarning($"[GameTemplateImportService] Import cancelled: {template.displayName} {template.version}");
                tcs.TrySetResult(false);
            };

            onFailed = (packageName, error) =>
            {
                if (!Matches(packageName)) return;
                Unsubscribe();
                tcs.TrySetException(new Exception(error));
            };

            AssetDatabase.importPackageCompleted += onCompleted;
            AssetDatabase.importPackageCancelled += onCancelled;
            AssetDatabase.importPackageFailed    += onFailed;

            AssetDatabase.ImportPackage(packagePath, true);

            return tcs.Task;
        }

        private async Task<string> DownloadPackage(string url, DownloadProgress progress, CancellationToken token)
        {
            // The package keeps its published file name - GameTemplateImportLogic.IsGameTemplatePackage
            // matches on the "game-template" prefix, and the post-import step (define symbols +
            // build scenes) is skipped for anything that does not match. A per-download folder
            // keeps the name intact while still avoiding collisions.
            var workingDir = Path.Combine(Application.temporaryCachePath, $"gametemplate_{Guid.NewGuid():N}");
            Directory.CreateDirectory(workingDir);

            var tempPath = Path.Combine(workingDir, ResolveFileName(url));

            var handler = new HttpClientHandler
            {
                Proxy    = null,
                UseProxy = false, // avoid auto proxy may cause slow speed
            };

            using (var client = new HttpClient(handler))
            {
                // Template packages are 130-170 MB. HttpClient's 100s default timeout covers the
                // whole transfer, so any connection slower than ~1.7 MB/s gets the request aborted
                // mid-download. Cancellation is driven by the user's Cancel button instead.
                client.Timeout = Timeout.InfiniteTimeSpan;

                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                // ResponseHeadersRead: stream the body to disk instead of buffering 165 MB in memory.
                using var response = await client
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token)
                    .ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                progress.SetTotal(response.Content.Headers.ContentLength ?? 0L);

                using var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var destination = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None,
                    BUFFER_SIZE, useAsync: true);

                var buffer = new byte[BUFFER_SIZE];
                int read;

                // ConfigureAwait(false) is critical here. Unity installs a SynchronizationContext
                // that is pumped once per editor tick, so without it every one of the ~4200 awaits
                // in this loop would marshal its continuation back to the main thread and wait for
                // the next tick - capping the download at roughly one 80 KB chunk per frame
                // regardless of how fast the connection actually is.
                while ((read = await source.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
                {
                    await destination.WriteAsync(buffer, 0, read, token).ConfigureAwait(false);
                    progress.Advance(read);
                }
            }

            return tempPath;
        }

        private static string ResolveFileName(string url)
        {
            try
            {
                var fileName = Path.GetFileName(new Uri(url).LocalPath);
                if (!string.IsNullOrEmpty(fileName) && fileName.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
                {
                    return fileName;
                }
            }
            catch (UriFormatException)
            {
                // fall through to the generated name
            }

            return $"game-template_{Guid.NewGuid():N}.unitypackage";
        }

        private static void DeleteWorkingDirectory(string workingDir)
        {
            if (string.IsNullOrEmpty(workingDir) || !Directory.Exists(workingDir)) return;

            try
            {
                Directory.Delete(workingDir, true);
            }
            catch { }
        }

        /// <summary>
        /// Byte counters shared between the download thread and the main-thread progress bar.
        /// </summary>
        private class DownloadProgress
        {
            private long _total;
            private long _read;

            public void SetTotal(long total) => Interlocked.Exchange(ref _total, total);

            public void Advance(int bytes) => Interlocked.Add(ref _read, bytes);

            public float Normalized
            {
                get
                {
                    var total = Interlocked.Read(ref _total);
                    return total <= 0L ? 0f : Mathf.Clamp01(Interlocked.Read(ref _read) / (float)total);
                }
            }

            public string Describe()
            {
                const float MB = 1024f * 1024f;

                var total = Interlocked.Read(ref _total);
                var read  = Interlocked.Read(ref _read);

                if (total <= 0L)
                {
                    return $"Downloading... {read / MB:F1} MB";
                }

                return $"Downloading... {Normalized * 100f:F0}% ({read / MB:F1} / {total / MB:F1} MB)";
            }
        }
    }
}
