/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using UnityEngine;
using UnityEngine.Networking;

namespace Falcon.Modules.CDN
{
    public readonly struct CdnFile
    {
        private readonly FLocalFile file;

        public CdnFile(FLocalFile file, CdnItem item) : this(file, item.fileName, item.FolderSegments)
        {
        }
        
        public CdnFile(FLocalFile file, string fileName, string[] folderSegments)
        {
            this.file = file;
            this.fileName = fileName;
            this.folderSegments = folderSegments;
        }

        public readonly string fileName;
        public readonly string[] folderSegments;
        
        public string FilePath => file.FilePath; 

        public Task<Stream> GetStream(CancellationToken token = default)
        {
            return file.LoadAsync(token);
        }

        public Task<byte[]> GetBytes(CancellationToken token = default)
        {
            return file.LoadBytesAsync(token);
        }

        public Task<string> GetString(Encoding encoding = null, CancellationToken token = default)
        {
            return file.LoadStringAsync(encoding, token);
        }

        public Task<T> GetJson<T>(Encoding encoding = null, CancellationToken token = default)
        {
            return file.LoadJsonAsync<T>(encoding, token);
        }

        public async Task<Texture2D> GetTexture(CancellationToken token = default)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(await GetBytes(token));
            return tex;
        }

        public async Task<Sprite> GetSprite(
            float pixelsPerUnit = 100f,
            Vector2? pivot = null,
            Rect? rect = null,
            bool markNonReadable = false,
            CancellationToken token = default)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(await GetBytes(token), markNonReadable);

            var spriteRect = rect ?? new Rect(0, 0, tex.width, tex.height);
            var spritePivot = pivot ?? new Vector2(0.5f, 0.5f);

            return Sprite.Create(
                tex,
                spriteRect,
                spritePivot,
                pixelsPerUnit
            );
        }

        public async Task<AudioClip> GetAudioClip(AudioType audioType, CancellationToken token = default)
        {
            using var www = UnityWebRequestMultimedia.GetAudioClip("file://" + file.FilePath, audioType);
            var request = www.SendWebRequest();
            while (!request.isDone)
                await Task.Yield();

            return www.result == UnityWebRequest.Result.Success
                ? DownloadHandlerAudioClip.GetContent(www)
                : throw new IOException(www.error);
        }

        public async Task<AssetBundle> GetAssetBundle(CancellationToken token = default)
        {
            var bundleRequest = AssetBundle.LoadFromMemoryAsync(await GetBytes(token));
            while (!bundleRequest.isDone)
                await Task.Yield();

            var bundle = bundleRequest.assetBundle;
            if (bundle is null) throw new IOException("Can't load AssetBundle from file: " + file.FilePath);
            return bundle;
        }

        public async Task<T> GetAssetFromBundle<T>(string assetName, CancellationToken token = default) where T : Object
        {
            var bundle = await GetAssetBundle(token);
            var loadRequest = bundle.LoadAssetAsync<T>(assetName);

            while (!loadRequest.isDone)
                await Task.Yield();

            var asset = loadRequest.asset as T;
            if (asset == null)
                throw new IOException(
                    $"Can't load asset '{assetName}' of type {typeof(T).Name} from AssetBundle: {file.FilePath}");

            return asset;
        }
    }
}