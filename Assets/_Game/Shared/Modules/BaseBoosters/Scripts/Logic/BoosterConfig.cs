/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-29
 */

using Falcon.Helpers.EventBus;
using Falcon.Modules.UnityLocalization.Runtime;
using Falcon.Shared.Common;

namespace Falcon.Shared.BaseBooster
{
    /// <summary>
    /// Đọc remote config booster theo type: levelUnlockBooster{type}, priceBooster{type}.
    /// </summary>
    public static class BoosterConfig
    {
        public static int LevelUnlock(string type) => GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, $"level_unlock_{type}");
        public static int GoldPrice(string type) => GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, $"price_{type}");
        public static int DefaultQuantity(string type) => GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, $"default_quantity_{type}");
        public static string GetName(string type) => ULocManager.GetLocalizedString($"name_{type}");
        public static string GetDescription(string type) => ULocManager.GetLocalizedString($"description_{type}");

    }
}
