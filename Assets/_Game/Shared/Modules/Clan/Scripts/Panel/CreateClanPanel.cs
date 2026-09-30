using Falcon.Modules.Core.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class CreateClanPanel : MonoBehaviour
    {
        [SerializeField] private EditClanController _editController;
        [SerializeField] private TextMeshProUGUI _txtPrice;

        private CSCreateClan _csCreateClan;

        private void OnEnable()
        {
            _editController?.Init(new ClanEditingData());
            if (_txtPrice != null) _txtPrice.text = Center.GetOrCreate<ClanService>().ClanCreateFee + string.Empty;
            _csCreateClan = null;
        }

        public void CreateOnClick()
        {
            var data = _editController?.EditingData;
            if (!Center.GetOrCreate<ClanService>().ClanInfoAvailable(data)) return;

            if (_csCreateClan != null)
            {
                Center.GetOrCreate<ClanService>().ShowToastWaitAMomentAfterOnClick();
                return;
            }

            if (!Center.GetOrCreate<ClanService>().CanCreateClan()) return;

            var cs = new CSCreateClan(data.avatar_id, data.name, data.description, data.open, data.required_level);
            _csCreateClan = cs;
            cs.AddSCListenerExt<SCResponse_Clan>((csMessage, scMessage, timeout, success) =>
            {
                if (csMessage == _csCreateClan)
                {
                    if (success && scMessage != null)
                    {
                        if (scMessage.success)
                        {
                            Center.GetOrCreate<ClanService>().PayClanCreationFee();
                            Center.GetOrCreate<ClanService>().ShowToastSuccess();
                        }
                        else
                        {
                            Center.GetOrCreate<ClanService>().ShowToast(scMessage.message);
                        }
                    }
                    else if (timeout) Center.GetOrCreate<ClanService>().ShowToastTimeout();
                    else Center.GetOrCreate<ClanService>().ShowToastFailed();

                    _csCreateClan = null;
                }

            }, int.MaxValue).Send();
        }
    }
}
