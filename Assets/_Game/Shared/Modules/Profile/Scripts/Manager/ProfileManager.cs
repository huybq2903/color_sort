/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-03
 */

using System;
using System.Text.RegularExpressions;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Shared.Addressable;
using UnityEngine;
using UnityEngine.U2D;

namespace Game.Shared.Profile
{
    public static class ProfileManager
    {
        public const string EVENT_SAVED = "falcon.modules.ui.edit_profile_click_save";

        private static SOProfileConfig _config;
        private static SpriteAtlas _atlas;

        public static SOProfileConfig Config
        {
            get
            {
                _config ??= Resources.Load<SOProfileConfig>("SO_Profile_Config");
                return _config;
            }
        }

        public static SpriteAtlas Atlas
        {
            get
            {
                _atlas ??= AddressableExtensions.Load<SpriteAtlas>(Config.nameAtlas);
                return _atlas;
            }
        }

        // id thưa (frame nhảy 4 -> 6) nên tra theo field id, không theo vị trí trong mảng
        public static AvatarConfig FindAvatar(int id) => Array.Find(Config.avatarConfigs, c => c.id == id);

        public static FrameConfig FindFrame(int id) => Array.Find(Config.frameConfigs, c => c.id == id);

        public static Sprite GetAvatar(int id)
        {
            var cfg = FindAvatar(id);
            return cfg == null ? null : Atlas.GetSprite(cfg.nameSprite);
        }

        public static Sprite GetFrame(int id)
        {
            var cfg = FindFrame(id);
            return cfg == null ? null : Atlas.GetSprite(cfg.nameSprite);
        }

        public static bool IsAvatarUnlock(int indexAvatar) =>
            (FindAvatar(indexAvatar)?.@default ?? false) ||
            FGameDataProfile.Instance.avatarResource.unlocks.Contains(indexAvatar);
        public static bool IsFrameUnlock(int indexFrame) =>
            (FindFrame(indexFrame)?.@default ?? false) ||
            FGameDataProfile.Instance.frameResource.unlocks.Contains(indexFrame);

        public static bool IsValidName(this string name)
        {
            return !string.IsNullOrWhiteSpace(name) &&
                   name.Length >= 3 &&
                   name.Length <= 20 &&
                   !Regex.IsMatch(name, "[\\/:\"*?<>|!@#,]");
        }

        public static bool IsUsingDefaultName()
        {
            string playerName = FGameDataProfile.Instance.playerName;
            if (string.IsNullOrEmpty(playerName))
                return true;

            return Regex.IsMatch(playerName, @"^Player_\d{4}$");
        }

        public static void Initialize()
        {
            ResourceCollector.Instance.AddToResourcesMap(FGameDataProfile.Instance.avatarResource, FGameDataProfile.Instance);
            ResourceCollector.Instance.AddToResourcesMap(FGameDataProfile.Instance.frameResource, FGameDataProfile.Instance);

            GameEvent<int>.Register("game.profile.open", OpenProfile, null);
        }

        private static void OpenProfile(int code)
        {
            PopupProfile.Show(p => p.SetCode(code));
        }
    }
}