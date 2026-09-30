/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-03
 */

using System;
using UnityEngine;

namespace Game.Shared.Profile
{
    [CreateAssetMenu(fileName = "SO_ArrowProfile_Config", menuName = "Scriptable Objects/SOProfileConfig")]
    public class SOProfileConfig : ScriptableObject
    {
        public string nameAtlas;

        // Mảng chứ không phải Dictionary: id thưa nên tra theo field id, không theo vị trí
        public AvatarConfig[] avatarConfigs;
        public FrameConfig[] frameConfigs;
    }

    [Serializable]
    public class AvatarConfig : BaseProfileElementConfig { }

    [Serializable]
    public class FrameConfig : BaseProfileElementConfig { }

    [Serializable]
    public class BaseProfileElementConfig
    {
        public int id;
        public string nameSprite;
        public bool @default;
    }

    public abstract class UIProfileElement : MonoBehaviour
    {
        public abstract void ActiveItem(int id);
    }
}
