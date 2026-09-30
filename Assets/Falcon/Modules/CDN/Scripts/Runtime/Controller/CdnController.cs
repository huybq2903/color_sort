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
    public class CdnController : MySingleton<CdnController>
    {
        private readonly ICdnService _service;

        public CdnController(ICdnService service)
        {
            _service = service;
        }

        public Task<CdnFile> LoadFile(string fileName, string folder = null, CancellationToken token = default,
            string key = null)
        {
            return _service.LoadFile(fileName, folder, token, key);
        }

        public Task<CdnFile> LoadFile(string fileName, string[] folderSegments = null,
            CancellationToken token = default,
            string key = null)
        {
            return _service.LoadFile(fileName, folderSegments, token, key);
        }

        public Task PrepareAll(CancellationToken token = default, string key = null)
        {
            return _service.PrepareAll(token, key);
        }

        public Task PrepareFolderExact(string folder, CancellationToken token = default, string key = null)
        {
            return _service.PrepareFolderExact(folder, token, key);
        }

        public Task PrepareFolderExact(string[] folderSegments, CancellationToken token = default, string key = null)
        {
            return _service.PrepareFolderExact(folderSegments, token, key);
        }

        public Task PrepareFolderPrefix(string folder, CancellationToken token = default, string key = null)
        {
            return _service.PrepareFolderPrefix(folder, token, key);
        }

        public Task PrepareFolderPrefix(string[] folderSegments, CancellationToken token = default, string key = null)
        {
            return _service.PrepareFolderPrefix(folderSegments, token, key);
        }

        public Task PrepareExact(string[] itemNames, CancellationToken token = default, string key = null)
        {
            return _service.PrepareExact(itemNames, token, key);
        }

        public Task<IEnumerable<CdnItemResponse>> GetRemoteItems(CancellationToken token = default, string key = null)
        {
            return _service.GetRemoteItems(token, key);
        }

        public event Action OnInitComplete
        {
            add => _service.OnInitComplete += value;
            remove => _service.OnInitComplete -= value;
        }
    }
}