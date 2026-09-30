using Cysharp.Threading.Tasks;
using Falcon.Modules.Core.Network;
using System;
using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class LeaveConfirmPanel : MonoBehaviour
    {
        private bool _leaving;
        public static Action onConfirmSuccess = null;

        private void OnEnable()
        {
            Center.GetOrCreate<ClanService>().OnClanChanged += OnLeaveClan;
        }

        private void OnDisable()
        {
            Center.GetOrCreate<ClanService>().OnClanChanged -= OnLeaveClan;
        }

        public void OnShow()
        {
            SetOwnerActive(true);
        }

        public void OnClose()
        {
            SetOwnerActive(false);
            _leaving = false;
        }

        public void OnConfirm()
        {
            if (_leaving)
            {
                Center.GetOrCreate<ClanService>().ShowToastWaitAMomentAfterOnClick();
                return;
            }

            _leaving = true;
            DoLeave().Forget();
        }

        // Qua UniTask thay vi tu gui CSLeaveClan/nghe SCResponse_Clan; gop con success/that bai
        private async UniTaskVoid DoLeave()
        {
            try
            {
                var sc = await Center.GetOrCreate<ClanService>().Leave(this.GetCancellationTokenOnDestroy());
                if (sc == null) { Center.GetOrCreate<ClanService>().ShowToastFailed(); return; }
                Center.GetOrCreate<ClanService>().ShowToastSuccess();
                // OnClanChanged (OnLeaveClan) tu dong panel + goi onConfirmSuccess khi MyClanCode ve 0
            }
            finally
            {
                _leaving = false;
            }
        }

        private void OnLeaveClan()
        {
            // OnClanChanged ban ca luc join, chi xu ly khi that su da roi clan (MyClanCode ve 0)
            if (Center.GetOrCreate<ClanService>().MyClanCode != 0) return;
            OnClose();
            onConfirmSuccess?.Invoke();
            onConfirmSuccess = null;
        }

        private void SetOwnerActive(bool active)
        {
            if (gameObject != null && gameObject.activeSelf != active) gameObject.SetActive(active);
        }
    }
}
