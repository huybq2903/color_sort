/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

namespace Falcon.Modules.CDN
{
    public interface ICdnService : IMySingleton
    {
        event Action OnInitComplete;

        Task<CdnFile> LoadFile(string fileName, string[] folderSegments = null, CancellationToken token = default,
            string key = null);

        Task PrepareAll(CancellationToken token = default, string key = null);
        Task PrepareFolderExact(string[] folderSegments, CancellationToken token = default, string key = null);
        Task PrepareFolderPrefix(string[] folderSegments, CancellationToken token = default, string key = null);
        Task PrepareExact(string[] itemNames, CancellationToken token = default, string key = null);
        Task<IEnumerable<CdnItemResponse>> GetRemoteItems(CancellationToken token = default, string key = null);
    }

    public static class CdnServiceExtensions
    {
        public static Task<CdnFile> LoadFile(this ICdnService service, string fileName, string folder = null,
            CancellationToken token = default,
            string key = null)
        {
            folder ??= "";
            var folderSplit = folder.Split(new[] { ',', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            return service.LoadFile(fileName, folderSplit, token, key);
        }

        public static Task PrepareFolderExact(this ICdnService service, string folder,
            CancellationToken token = default, string key = null)
        {
            folder ??= "";
            var folderSplit = folder.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            return service.PrepareFolderExact(folderSplit, token, key);
        }

        public static Task PrepareFolderPrefix(this ICdnService service, string folder,
            CancellationToken token = default, string key = null)
        {
            folder ??= "";
            var folderSplit = folder.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            return service.PrepareFolderPrefix(folderSplit, token, key);
        }
    }
}