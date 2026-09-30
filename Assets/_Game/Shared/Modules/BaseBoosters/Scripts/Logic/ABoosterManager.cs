/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-28
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Helpers.FReflection;
using Falcon.Shared.BaseInGame;
using Falcon.Shared.Tutorial;
using Sirenix.OdinInspector;
using UnityEngine;
using Falcon.Shared.Common;

namespace Falcon.Shared.BaseBooster
{
    public abstract class ABoosterManager : MonoBehaviour, ISubManager, ILevelAfterSpawn
    {
        [ShowInInspector, ReadOnly] protected readonly Dictionary<string, ABoosterLogic> _dictLogics = new();
        [field: Inject] protected ATutorialManager TutorialManager { get; }

        public ABoosterLogic GetBoosterLogic(string type) => _dictLogics.GetValueOrDefault(type);

        public virtual void Initialized()
        {
            GameEvent<string>.Register(GameKeys.USE_BOOSTER, UseBooster, this);
            GameEvent<string>.Register(GameKeys.USE_BOOSTER_BY_REVIVE, UseBoosterByRevive, this);

            var types = FReflection.Instance.GetTypes().Where(t => typeof(ABoosterLogic).IsAssignableFrom(t) && !t.IsAbstract);
            foreach (var t in types)
            {
                if (Activator.CreateInstance(t) is not ABoosterLogic logic) continue;
                _dictLogics[logic.Type] = logic;
                logic.Initialize(this);
            }
        }

        public UniTask AfterSpawn()
        {
            OnUnlockBoosterIfHad();
            return UniTask.CompletedTask;
        }

        protected virtual void OnUnlockBoosterIfHad()
        {
            foreach (var logic in _dictLogics.Values)
            {
                if (!logic.NeedUnlockTutorial) continue;

                if (TutorialManager.PreloadTutorial($"unlock_{logic.Type}") is not ATutorialUnlockBooster tutorial)
                    Debug.LogWarning($"[ABoosterManager] Thiếu tutorial 'unlock_{logic.Type}'.");
                else
                {
                    tutorial.BoosterLogic = logic;
                    tutorial.StartTutorial();
                }
            }
        }

        protected virtual void UseBoosterByRevive(string type) => _dictLogics.GetValueOrDefault(type)?.Execute();

        protected virtual void UseBooster(string type)
        {
            if (TutorialManager.CurrentTutorial is ATutorialConfirmUseBooster confirm && confirm.BoosterLogic.Type == type)
            {
                confirm.ForceStopTutorial();
                return;
            }

            var logic = _dictLogics.GetValueOrDefault(type);
            if (logic == null ||
                !logic.BeforeExecute() ||
                TutorialManager.CurrentTutorial is ATutorialUnlockBooster unlock && unlock.BoosterLogic.Type == type)
            {
                return;
            }

            if (TutorialManager.PreloadTutorial($"confirm_use_{type}") is not ATutorialConfirmUseBooster confirmUse)
            {
                logic.Execute();
                logic.AfterExecute();
                return;
            }

            confirmUse.BoosterLogic = logic;
            confirmUse.StartTutorial();
        }

        protected virtual void OnDisable()
        {
            GameEvent<string>.Unregister(GameKeys.USE_BOOSTER, UseBooster, this);
            GameEvent<string>.Unregister(GameKeys.USE_BOOSTER_BY_REVIVE, UseBoosterByRevive, this);
            foreach (var logic in _dictLogics.Values)
                logic.Dispose();
        }
    }
}
