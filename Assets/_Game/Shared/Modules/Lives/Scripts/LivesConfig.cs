// Author: Bui Quang Huy
// Company: Falcon Games

using Falcon.Helpers.EventBus;
using Falcon.Shared.Common;

namespace Falcon.Shared.Lives
{
    /// <summary>Đọc remote config qua EventBus, để module không phải phụ thuộc GameConfig.</summary>
    internal static class LivesConfig
    {
        /// GameConfig đăng ký request này trong Init, chưa chạy thì trả fallback.
        public static int Get(string key, int fallback = 0)
        {
            return GameRequest<(string, int), int>.TryRequest(GameKeys.GET_REMOTE_CONFIG, (key, fallback), out var value)
                ? value
                : fallback;
        }
    }
}
