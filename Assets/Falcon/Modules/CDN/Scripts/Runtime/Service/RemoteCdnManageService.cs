/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

namespace Falcon.Modules.CDN
{
    public class RemoteCdnManageService : IMySingleton
    {
        private const string kCdnDomain = "https://cdn-gateway.data4game.com/api/v1/cdn/client/dev-src/packages";
        private readonly IFAppInfoRepository _appInfoRepository;
        private readonly FCentralUserParamService _paramService;
        private readonly CdnSetting _setting;

        public RemoteCdnManageService(CdnSetting setting, IFAppInfoRepository appInfoRepository,
            FCentralUserParamService paramService)
        {
            _setting = setting;
            _appInfoRepository = appInfoRepository;
            _paramService = paramService;
        }

        public async Task<CdnItemResponse> GetByLocation(string fileName, string[] folderSegments = null,
            CancellationToken cancellationToken = default, string key = null)
        {
            folderSegments ??= Array.Empty<string>();
            var request = BaseRequest(HttpMethod.Get, "/files/" + string.Join('/', folderSegments) + '/' + fileName,
                key);

            var response = await request.Execute(cancellationToken);
            return await response.SuccessObj<CdnItemResponse>();
        }

        public async Task<IEnumerable<CdnItemResponse>> GetAll(CancellationToken cancellationToken = default,
            string key = null)
        {
            var request = BaseRequest(HttpMethod.Post, "/files", key)
                .SetJsonBody(_paramService.GetUserParams());

            var response = await request.Execute(cancellationToken);
            return ReadStream(await response.SuccessStreamBody());
        }

        public async Task<IEnumerable<CdnItemResponse>> GetByLocations(List<CdnItem> ids,
            CancellationToken cancellationToken = default, string key = null)
        {
            var request = BaseRequest(HttpMethod.Post, "/files/by-locations", key)
                .SetJsonBody(ids);

            var response = await request.Execute(cancellationToken);
            return ReadStream(await response.SuccessStreamBody());
        }

        public async Task<IEnumerable<CdnItemResponse>> GetByFolderExact(string[] folderSegments,
            CancellationToken cancellationToken = default, string key = null)
        {
            var request = BaseRequest(HttpMethod.Post, "/folders/query-exact/" + string.Join('/', folderSegments), key)
                .SetJsonBody(_paramService.GetUserParams());

            var response = await request.Execute(cancellationToken);
            return ReadStream(await response.SuccessStreamBody());
        }

        public async Task<IEnumerable<CdnItemResponse>> GetByFolderPrefix(string[] folderSegments,
            CancellationToken cancellationToken = default, string key = null)
        {
            var request = BaseRequest(HttpMethod.Post, "/folders/query-prefix/" + string.Join('/', folderSegments), key)
                .SetJsonBody(_paramService.GetUserParams());

            var response = await request.Execute(cancellationToken);
            return ReadStream(await response.SuccessStreamBody());
        }

        private static List<CdnItemResponse> ReadStream(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var results = new List<CdnItemResponse>();
            while (reader.ReadLine() is { } line) 
                results.Add(line.JsonToObj<CdnItemResponse>());
            return results;
        }

        private HttpRequest BaseRequest(HttpMethod method, string postPath, string key)
        {
            return new GeneralHttpRequest(kCdnDomain + '/' + _appInfoRepository.PackageName + postPath, method)
                .AddHeader("Key", CheckKey(key));
        }

        private string CheckKey(string key)
        {
            return key ?? _setting.GetKey();
        }
    }
}