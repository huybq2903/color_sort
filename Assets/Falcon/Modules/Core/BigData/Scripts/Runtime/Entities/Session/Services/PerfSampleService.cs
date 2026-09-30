/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using Falcon.Helpers.Devkit;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Đo hiệu năng quãng chơi để đóng vào bản tin session_data lúc khép (§D7): zero event mới,
    /// zero volume mới. Phải là MonoBehaviour vì đếm cú tụt khung thì bắt buộc nhìn từng frame —
    /// sampler 1Hz chỉ thấy trung bình, không thấy cú giật.
    /// <br/>Update ở đây cố tình chỉ có vài phép cộng; mọi luật nằm ở <see cref="PerfSampleState"/>.
    /// Chạy nền thì Unity không gọi Update nên số liệu tự nhiên chỉ tính lúc foreground.
    /// </summary>
    [NoLazy]
    public class PerfSampleService : MonoSingleton<PerfSampleService>
    {
        private readonly PerfSampleState _state = new();

        private void OnEnable()
        {
            Application.lowMemory += OnLowMemory;
        }

        private void OnDisable()
        {
            Application.lowMemory -= OnLowMemory;
        }

        private void Update()
        {
            // unscaled: game chỉnh Time.timeScale (pause menu, slow-mo) không được làm lệch fps đo được
            _state.AddFrame(Time.unscaledDeltaTime);
        }

        private void OnLowMemory()
        {
            _state.AddMemoryWarning();
        }

        /// <summary>Lấy tóm tắt của quãng vừa rồi và reset — gọi lúc dựng bản tin session_data.</summary>
        public PerfSummary TakeSummary()
        {
            return _state.TakeAndReset();
        }
    }
}
