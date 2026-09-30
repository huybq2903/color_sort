/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-03
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Game.Shared.Profile
{
    public class UIProfile_Avatar : UIProfileElement
    {
        private Image _image;
        private string _url;

        private void SetUrl(string urlImage)
        {
            _url = urlImage;
        }
        
        private async void UpdateImageUrl()
        {
            var sprite = await ImageWebExtensions.GetSpriteAsync(_url);
            if (sprite)
            {
                _image.sprite = sprite;
            }
        }

        public override void ActiveItem(int id)
        {
            _image ??= GetComponentInChildren<Image>();
            if (id >= 0)
            {
                _image.sprite = ProfileManager.GetAvatar(id);
            }
            else
            {
                UpdateImageUrl();
            }
        }
    }
}

public static class ImageWebExtensions
{
    private static readonly Dictionary<string, Sprite> _cache = new();

    public static async Task<Sprite> GetSpriteAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        if (_cache.TryGetValue(url, out var sprite))
            return sprite;

        using var uwr = UnityWebRequestTexture.GetTexture(url);
        var op = uwr.SendWebRequest();

        while (!op.isDone) await Task.Yield(); // đợi non-blocking

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Load image failed: {uwr.error}");
            return null;
        }

        var texture = DownloadHandlerTexture.GetContent(uwr);
        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        _cache[url] = sprite;

        return sprite;
    }

    public static void ClearCache()
    {
        foreach (var s in _cache.Values)
        {
            if (s != null)
            {
                Object.Destroy(s.texture);
                Object.Destroy(s);
            }
        }
        _cache.Clear();
    }
}