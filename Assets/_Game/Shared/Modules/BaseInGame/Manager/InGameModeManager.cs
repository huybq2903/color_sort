/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-22
 */

using System;
using System.Linq;
using System.Reflection;
using Falcon.Helpers.FReflection;
using Falcon.Shared.Addressable;
using Falcon.Shared.Common;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Shared.BaseInGame
{
    /// <summary>Builds the mode requested via DataTemp: Gameplay prefab, HUD, then the mode itself.</summary>
    [DefaultExecutionOrder(-200)]
    public class InGameModeManager : MonoBehaviour
    {
        [SerializeField] private Transform hudRoot;

        [ShowInInspector, ReadOnly] public string Mode { get; private set; }

        public AModeGame Current { get; private set; }

        private void Awake()
        {
            Mode = DataTempExtensions<string>.Get(GameModeKeys.MODE);
            if (string.IsNullOrEmpty(Mode)) Mode = GameModeKeys.CLASSIC;

            var type = FReflection.Instance.GetTypes(typeof(AModeGame))
                .First(t => !t.IsAbstract && t.GetCustomAttribute<GameModeAttribute>()?.Mode == Mode);
            Current = (AModeGame)Activator.CreateInstance(type);

            // Instantiate runs the prefab's Awake, so its managers are injected and initialized here.
            var gameplay = Instantiate(AddressableExtensions.Load<GameObject>(GameModeKeys.GAMEPLAY_PREFIX + Mode));

            Current.Hud = Instantiate(AddressableExtensions.Load<GameObject>(GameModeKeys.HUD_PREFIX + Mode), hudRoot, false);
            Current.Hud.transform.SetAsFirstSibling(); // behind popups

            gameplay.GetComponent<InGameManager>().Add(Current);
        }

        // Rời GameScene là hết mode. Sót key lại thì ván thường ngay sau đó bị tính như mode mà không có gì báo.
        private void OnDestroy()
        {
            Current?.OnExit();
            DataTempExtensions<string>.Remove(GameModeKeys.MODE);
        }
    }
}
