/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

namespace Falcon.Modules.CDN
{
    public class CdnService : ICdnService, IInit
    {
        private readonly ConcurrentDictionary<CdnItem, (ExecState state, FLocalFile file)> _fileStates = new();
        private readonly LocalCdnRepository _local;
        private readonly RemoteCdnManageService _remote;
        private readonly CdnSetting _setting;

        public CdnService(LocalCdnRepository local, RemoteCdnManageService remote, CdnSetting setting)
        {
            _local = local;
            _remote = remote;
            _setting = setting;
        }

        public event Action OnInitComplete;

        public async Task<CdnFile> LoadFile(string fileName, string[] folderSegments = null,
            CancellationToken token = default,
            string key = null)
        {
            var item = new CdnItem(fileName, folderSegments);
            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (TryStart(item))
                    return await LoadFileInternal(item, token, key);

                if (_fileStates.TryGetValue(item, out var tuple) && tuple.state.IsSuccess())
                    return new CdnFile(tuple.file, fileName, folderSegments);

                await Task.Yield();
            }
        }

        public async Task PrepareAll(CancellationToken token = default, string key = null)
        {
            LocalCdnRepository.CleanUpEmptyFolders();
            var localFiles = _local.ListFilesUsingFolderPrefix(Array.Empty<string>());
            try
            {
                var responses =
                    await RetryUtils.RetryTask(() => _remote.GetAll(token, key), _setting.RetryAttempts, token);
                foreach (var response in CheckLocalFilesForUpdates(responses, localFiles))
                    await TryUpdateItem(response, token);
            }
            catch (HttpRequestException e)
            {
                CdnLogger.Instance.Warning("Failed to connect to server, using files already have on local", e);

                foreach (var (cdnItem, file) in localFiles)
                    _fileStates.Compute(cdnItem, tuple =>
                    {
                        if (tuple.HasValue && tuple.Value.state.IsSuccess()) return tuple;
                        return (ExecState.Succeed, file);
                    });
            }
        }

        public async Task PrepareFolderExact(string[] folderSegments, CancellationToken token = default,
            string key = null)
        {
            LocalCdnRepository.CleanUpEmptyFolders();
            var localFiles = _local.ListFilesAtFolderExact(folderSegments);
            try
            {
                var responses = await RetryUtils.RetryTask(() => _remote.GetByFolderExact(folderSegments, token, key),
                    _setting.RetryAttempts,
                    token);
                foreach (var response in CheckLocalFilesForUpdates(responses, localFiles))
                    await TryUpdateItem(response, token);
            }
            catch (HttpRequestException e)
            {
                CdnLogger.Instance.Warning("Failed to connect to server, using files already have on local", e);

                foreach (var (cdnItem, file) in localFiles)
                    _fileStates.Compute(cdnItem, tuple =>
                    {
                        if (tuple.HasValue && tuple.Value.state.IsSuccess()) return tuple;
                        return (ExecState.Succeed, file);
                    });
            }
        }

        public async Task PrepareFolderPrefix(string[] folderSegments, CancellationToken token = default,
            string key = null)
        {
            LocalCdnRepository.CleanUpEmptyFolders();
            var localFiles = _local.ListFilesUsingFolderPrefix(folderSegments);
            try
            {
                var responses = await RetryUtils.RetryTask(() => _remote.GetByFolderPrefix(folderSegments, token, key),
                    _setting.RetryAttempts, token);
                foreach (var response in CheckLocalFilesForUpdates(responses, localFiles))
                    await TryUpdateItem(response, token);
            }
            catch (HttpRequestException e)
            {
                CdnLogger.Instance.Warning("Failed to connect to server, using files already have on local", e);

                foreach (var (cdnItem, file) in localFiles)
                    _fileStates.Compute(cdnItem, tuple =>
                    {
                        if (tuple.HasValue && tuple.Value.state.IsSuccess()) return tuple;
                        return (ExecState.Succeed, file);
                    });
            }
        }

        public async Task PrepareExact(string[] itemNames, CancellationToken token = default, string key = null)
        {
            LocalCdnRepository.CleanUpEmptyFolders();
            var localFiles = _local.ListFilesUsingFolderPrefix(Array.Empty<string>());
            try
            {
                // get all first
                var responses = await RetryUtils.RetryTask(() => _remote.GetAll(token, key),
                    _setting.RetryAttempts, token);
                foreach (var response in CheckLocalFilesForUpdates(itemNames, responses, localFiles))
                    await TryUpdateItem(response, token);
            }
            catch (HttpRequestException e)
            {
                CdnLogger.Instance.Warning("Failed to connect to server, using files already have on local", e);

                foreach (var (cdnItem, file) in localFiles)
                    _fileStates.Compute(cdnItem, tuple =>
                    {
                        if (tuple.HasValue && tuple.Value.state.IsSuccess()) return tuple;
                        return (ExecState.Succeed, file);
                    });
            }
        }

        public async Task<IEnumerable<CdnItemResponse>> GetRemoteItems(CancellationToken token = default,
            string key = null)
        {
            return await RetryUtils.RetryTask(() => _remote.GetAll(token, key), _setting.RetryAttempts, token);
        }

        [SuppressMessage("ReSharper", "MethodSupportsCancellation")]
        public Task Init(CancellationToken cancellationToken = default)
        {
            if (_setting.SyncAllFilesAutomatically)
                Task.Run(() => PrepareAll().ContinueWith(_ => OnInitComplete?.Invoke()));
            return Task.CompletedTask;
        }

        private async Task<CdnFile> LoadFileInternal(CdnItem item, CancellationToken token = default,
            string key = null)
        {
            try
            {
                var response = await RetryUtils.RetryTask(() => _remote.GetByLocation(item.fileName, item.FolderSegments, token, key),
                    _setting.RetryAttempts, token);
                var file = await RetryUtils.RetryTask(() => UpdateItem(response, token), _setting.RetryAttempts,
                    token);
                _fileStates[item] = (ExecState.Succeed, file);
                return new CdnFile(file, item);
            }
            catch (HttpRequestException e)
            {
                CdnLogger.Instance.Warning("Failed to connect to server, using files already have on local", e);
                var file = _local.GetFileShell(item);
                if (!file.Exists())
                {
                    CdnLogger.Instance.Error(e);
                    _fileStates[item] = (ExecState.Failed, null);
                    throw;
                }

                _fileStates.Compute(item, valueTuple =>
                {
                    if (valueTuple.HasValue && valueTuple.Value.state.IsSuccess()) return valueTuple;
                    return (ExecState.Succeed, file);
                });
                return new CdnFile(file, item);
            }
            catch (Exception ex)
            {
                CdnLogger.Instance.Error(ex);
                _fileStates[item] = (ExecState.Failed, null);
                throw;
            }
        }

        private async Task TryUpdateItem(CdnItemResponse response, CancellationToken cancellationToken = default)
        {
            if (!TryStart(response.CdnItem)) return;
            var key = response.CdnItem;
            try
            {
                var file = await RetryUtils.RetryTask(() => UpdateItem(response, cancellationToken),
                    _setting.RetryAttempts, cancellationToken);
                _fileStates[key] = (ExecState.Succeed, file);
                CdnLogger.Instance.Info("Cdn update file: " + key.ToJson());
            }
            catch (Exception ex)
            {
                CdnLogger.Instance.Error(ex);
                _fileStates[key] = (ExecState.Failed, null);
            }
        }

        private bool TryStart(CdnItem item)
        {
            var canRun = false;
            _fileStates.Compute(item, tuple =>
            {
                if (tuple.HasValue && !tuple.Value.state.CanStart()) return tuple;
                canRun = true;
                return (ExecState.Processing, null);
            });
            return canRun;
        }

        private async Task<FLocalFile> UpdateItem(CdnItemResponse response,
            CancellationToken cancellationToken = default)
        {
            var key = response.CdnItem;
            var shell = _local.GetFileShell(key);
            var httpResponse = await new GetRequest(response.url).Execute(cancellationToken);
            await using var stream = await httpResponse.SuccessStreamBody();
            await shell.SaveAsync(stream, token: cancellationToken);
            LocalFileHashRepository.SaveFileHash(shell, response.fileHash);
            return shell;
        }

        private IEnumerable<CdnItemResponse> CheckLocalFilesForUpdates(string[] filteredItemNames,
            IEnumerable<CdnItemResponse> responses,
            Dictionary<CdnItem, FLocalFile> localFiles)
        {
            var filtered = responses.Where(item => filteredItemNames.Contains(item.fileName));
            var filteredLocalFiles = localFiles
                .Where(entry => filteredItemNames.Contains(entry.Key.fileName))
                .ToDictionary(entry => entry.Key, entry => entry.Value);
            return CheckLocalFilesForUpdates(filtered, filteredLocalFiles);
        }

        private IEnumerable<CdnItemResponse> CheckLocalFilesForUpdates(IEnumerable<CdnItemResponse> responses,
            Dictionary<CdnItem, FLocalFile> localFiles)
        {
            foreach (var response in responses)
            {
                var item = response.CdnItem;
                if (localFiles.Remove(item, out var file) &&
                    string.Equals(LocalFileHashRepository.GetFileHash(file), response.fileHash))
                {
                    _fileStates[item] = (ExecState.Succeed, file);
                }
                else
                {
                    if (_fileStates.TryAdd(item, (ExecState.NotStarted, null))) yield return response;
                }
            }

            foreach (var file in localFiles.Values)
                try
                {
                    file.Delete();
                }
                catch (Exception ex)
                {
                    CdnLogger.Instance.Error(ex);
                }
        }
    }
}