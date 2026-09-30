using Falcon.Modules.Core.Network;
using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class EditClanPanel : MonoBehaviour
    {
        [SerializeField] private EditClanController _editController;
        private CSEditClanInfo _csEditClanInfo = null;

        private void OnEnable() { _csEditClanInfo = null; }

        public void InitClanData(ClanData data)
        {
            _editController?.Init(new ClanEditingData(data));
        }

        public void SaveEditOnClick()
        {
            if (_csEditClanInfo != null)
            {
                Center.GetOrCreate<ClanService>().ShowToastWaitAMomentAfterOnClick();
                return;
            }

            var editing = _editController?.EditingData;
            if (editing == null) return;
            if (!Center.GetOrCreate<ClanService>().ClanInfoAvailable(editing)) return;

            var cs = new CSEditClanInfo(editing.avatar_id, editing.name, editing.description, editing.open, editing.required_level);
            _csEditClanInfo = cs;
            cs.AddSCListenerExt<SCResponse_Clan>((csMessage, scMessage, timeout, success) =>
            {
                if (csMessage == _csEditClanInfo)
                {
                    if (scMessage != null)
                    {
                        if (scMessage.success)
                        {
                            Center.GetOrCreate<ClanService>().ShowToastSuccess();
                            OnClose();
                        }
                        else
                        {
                            Center.GetOrCreate<ClanService>().ShowToast(scMessage.message);
                        }
                    }
                    else if (timeout) Center.GetOrCreate<ClanService>().ShowToastTimeout();
                    else Center.GetOrCreate<ClanService>().ShowToastFailed();

                    _csEditClanInfo = null;
                }
            }, int.MaxValue).Send();
        }

        public void OnShow()
        {
            SetActiveSafe(gameObject, true);
        }

        public void OnClose()
        {
            SetActiveSafe(gameObject, false);
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go == null) return;
            if (go.activeSelf != active) go.SetActive(active);
        }
    }
}
