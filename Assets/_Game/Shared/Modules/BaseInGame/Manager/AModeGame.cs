/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-22
 */

using System;
using Falcon.Helpers.FReflection;
using UnityEngine;

namespace Falcon.Shared.BaseInGame
{
    public static class GameModeKeys
    {
        /// <summary>DataTemp string, set before loading GameScene. Unset = <see cref="CLASSIC"/>.</summary>
        public const string MODE = "game_mode";

        /// <summary>Addressable prefab addresses per mode: GAMEPLAY_PREFIX / HUD_PREFIX + mode.</summary>
        public const string GAMEPLAY_PREFIX = "Gameplay_";
        public const string HUD_PREFIX = "HUD_";

        public const string CLASSIC = "Classic";
        public const string EDITOR_PLAYTEST = "EditorPlaytest";
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GameModeAttribute : Attribute
    {
        public readonly string Mode;

        public GameModeAttribute(string mode) => Mode = mode;
    }

    [FReflection]
    public abstract class AModeGame : ISubManager
    {
        /// <summary>Spawned by InGameModeManager before OnEnter.</summary>
        public GameObject Hud { get; internal set; }

        void ISubManager.Initialized() => OnEnter();

        public virtual void OnEnter() { }

        public virtual void OnExit() { }
    }
}
