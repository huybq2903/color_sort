/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-03
 */

using Cysharp.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Shared.Common;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Shared.BaseInGame
{
    public enum GameState
    {
        Idle,
        Playing,
        Win,
        TempLose, // đang mời revive, chưa log kết quả, còn quay lại Playing được
        Lose,     // thua thật (bỏ cuộc), đã log
    }

    /// <summary>Máy trạng thái thắng/thua chung. Game con chỉ viết điều kiện.</summary>
    public abstract class AWinLoseManager : MonoBehaviour, ISubManager, ILevelBeforeSpawn
    {
        [ShowInInspector, ReadOnly] public GameState GameState { get; protected set; } = GameState.Idle;

        public bool IsPlaying => GameState == GameState.Playing;

        [Inject] protected ALevelManager _levelManager;

        protected FlowSequence _winFlow;
        protected FlowSequence _tempLoseFlow;

        public void Initialized()
        {
            _winFlow = Flow.Of(GameKeys.WIN_FLOW);
            _tempLoseFlow = Flow.Of(GameKeys.TEMP_LOSE_FLOW);

            _tempLoseFlow.Add(DoGiveUp, 999);

            GameEvent.Register(GameKeys.DO_WIN, DoWin, this);
            GameEvent.Register(GameKeys.DO_TEMP_LOSE, DoTempLose, this);
        }

        public UniTask BeforeSpawn()
        {
            GameState = GameState.Playing;
            if (!_levelManager.IsResume)
                GameEvent.Emit(GameKeys.USE_LIVE);
            return UniTask.CompletedTask;
        }

        /// <summary>Điều kiện thắng, chỉ được gọi khi đang Playing.</summary>
        protected abstract bool IsWin();

        /// <summary>Điều kiện thua kèm lý do cho popup, chỉ được gọi khi đang Playing.</summary>
        protected abstract bool IsLose();

        /// <summary>Gọi sau mỗi nước đi. Thắng xét trước thua.</summary>
        public void DoCheckWinLose()
        {
            if (!IsPlaying) return;
            if (IsWin())
                DoWin();
            else if (IsLose())
                DoTempLose();
        }

        public virtual void DoWin()
        {
            if (!IsPlaying) return;
            GameState = GameState.Win;
            GameEvent.Emit(GameKeys.RELEASE_CACHE_LIVE);
            GameEvent.Emit(GameKeys.WIN);
            _winFlow.Run().Forget();
        }

        /// <summary>Thua tạm: còn cửa revive, chưa log kết quả. State khác Playing tự khoá thao tác chơi.</summary>
        public virtual void DoTempLose()
        {
            if (!IsPlaying) return;
            GameState = GameState.TempLose;
            GameEvent.Emit(GameKeys.TEMP_LOSE);
            _tempLoseFlow.Run().Forget();
        }

        public virtual void DoRevive()
        {
            if (GameState != GameState.TempLose) return; // chỉ cứu được ván đang mời revive
            GameState = GameState.Playing;
            GameEvent.Emit(GameKeys.REVIVE);
        }

        public virtual void DoGiveUp()
        {
            if (GameState == GameState.Win || GameState == GameState.Lose) return;
            GameState = GameState.Lose;
            GameEvent.Emit(GameKeys.CLEAR_CACHE_LIVE);
            GameEvent.Emit(GameKeys.LOSE);
        }

        private void OnDestroy()
        {
            GameEvent.Unregister(GameKeys.DO_WIN, DoWin, this);
            GameEvent.Unregister(GameKeys.DO_TEMP_LOSE, DoTempLose, this);
            _winFlow.Clear();
            _tempLoseFlow.Clear();
        }
    }
}
