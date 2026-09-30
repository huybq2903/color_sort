using Falcon.Modules.Core.Network;
using System;
using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class RemoveConfirmPanel : MonoBehaviour
    {
        private int member_code = 0;
        private CSRemoveMemberInClan _csRemoveMemberInClan;
        public Action onSuccessAction = null;

        public void OnShow(int member_code)
        {
            this.member_code = member_code;
            gameObject.SetActive(true);
        }

        public void OnClose()
        {
            gameObject.SetActive(false);
            _csRemoveMemberInClan = null;
        }

        public void OnConfirm()
        {
            if (_csRemoveMemberInClan != null)
            {
                Center.GetOrCreate<ClanService>().ShowToastWaitAMomentAfterOnClick();
                return;
            }

            var cs = new CSRemoveMemberInClan(member_code);
            _csRemoveMemberInClan = cs;

            cs.AddSCListenerExt<SCResponse_Clan>((csMessage, scMessage, timeout, success) =>
            {
                if (csMessage == _csRemoveMemberInClan)
                {
                    if (success && scMessage != null)
                    {
                        if (scMessage.success)
                        {
                            onSuccessAction?.Invoke();
                            Center.GetOrCreate<ClanService>().ShowToastSuccess();
                            OnClose();
                        }
                        else
                        {
                            Center.GetOrCreate<ClanService>().ShowToast(scMessage.message);
                        }
                    }
                    else if (timeout)
                    {
                        Center.GetOrCreate<ClanService>().ShowToastTimeout();
                    }
                    else
                    {
                        Center.GetOrCreate<ClanService>().ShowToastFailed();
                    }
                }
                _csRemoveMemberInClan = null;
            }, int.MaxValue).Send();
        }
    }
}
