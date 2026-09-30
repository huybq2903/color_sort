using Falcon.Shared.BaseInGame;
using Falcon.Shared.Common;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Falcon.Shared.Performance
{
    /// <summary>Đo tỉ lệ frame trượt lúc chơi rồi hạ chất lượng theo nấc. Chỉ hạ; sống chết theo GameScene.</summary>
    public class InGameAdaptiveQuality : MonoBehaviour, ISubManager
    {
        private const float WarmupSeconds = 5f;
        private const float WindowSeconds = 3f;
        private const float DwellSeconds = 6f;

        /// <summary>Frame vượt ngân sách × hệ số này thì tính là trượt; 1.5 để bỏ qua dao động lặt vặt.</summary>
        private const float MissFactor = 1.5f;

        private const float MissRateToDrop = 0.30f;

        /// <summary>Nấc hạ: renderScale trước vì main thread đang rảnh, hạ fps là nước cuối.</summary>
        private static readonly float[] Scales = { 1f, 0.85f, 0.85f, 0.75f };

        private static readonly bool[] HalfRate = { false, false, true, true };

        [Inject] private AWinLoseManager _winLose;

        private int _step;
        private int _baseRate;
        private float _warmup;
        private float _window;
        private float _dwell;
        private int _frames;
        private int _missed;
        private bool _paused;

        /// <summary>Base rate đọc từ FrameRateSetup đã chạy lúc boot — giữ một nguồn sự thật duy nhất.</summary>
        public void Initialized()
        {
            _baseRate = Application.targetFrameRate;
            _warmup = WarmupSeconds;
            ApplyStep(0);
            ResetWindow();
        }

        /// <summary>Rời GameScene là trả nấc: Home nhẹ hơn gameplay, không việc gì chịu nấc của ván vừa rồi.</summary>
        private void OnDestroy()
        {
            ApplyStep(0);
        }

        private void Update()
        {
            if (_paused || _baseRate <= 0 || _winLose == null || !_winLose.IsPlaying) return;

            var dt = Time.unscaledDeltaTime;

            if (_warmup > 0f)
            {
                _warmup -= dt;
                return;
            }

            if (_dwell > 0f) _dwell -= dt;

            _frames++;
            if (dt > 1f / _baseRate * MissFactor) _missed++;

            _window += dt;
            if (_window < WindowSeconds) return;

            Evaluate();
            ResetWindow();
        }

        private void Evaluate()
        {
            if (_frames <= 0) return;

            var missRate = _missed / (float)_frames;
            if (missRate < MissRateToDrop || _dwell > 0f || _step >= Scales.Length - 1) return;

            Debug.Log($"[AdaptiveQuality] trượt {missRate:P0} — hạ nấc {_step} → {_step + 1}");
            ApplyStep(_step + 1);
            _dwell = DwellSeconds;
        }

        private void ApplyStep(int step)
        {
            _step = Mathf.Clamp(step, 0, Scales.Length - 1);

            if (_baseRate > 0)
                Application.targetFrameRate = HalfRate[_step] ? Mathf.Max(30, _baseRate / 2) : _baseRate;

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
                urp.renderScale = Scales[_step];
        }

        private void ResetWindow()
        {
            _window = 0f;
            _frames = 0;
            _missed = 0;
        }
    }
}
