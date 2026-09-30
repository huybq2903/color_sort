using Falcon.Modules.Core.Network;
using TMPro;
using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class HelpResourceRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameTxt;

        private HelpResourceRowData _data = null;
        private UIPopupHelpResource _popup = null;
        private int _index = -1;
        private CSReceiveHelpResource _csReceiveHelpResource = null;

        private void Awake()
        {
            if (_nameTxt != null) _nameTxt.overflowMode = TextOverflowModes.Ellipsis;
        }

        public void Init(HelpResourceRowData data, UIPopupHelpResource popup, int index)
        {
            _data = data;
            _popup = popup;
            _index = index;
            _csReceiveHelpResource = null;

            if (_nameTxt != null)
                _nameTxt.text = data?.player_name ?? string.Empty;
        }

        public void AddOnClick()
        {
            if (_csReceiveHelpResource != null)
                return;

            if (!Center.GetOrCreate<ClanService>().CanAddHelpResource())
                return;

            if (_data == null)
                return;

            var cs = new CSReceiveHelpResource(_data.player_code);
            _csReceiveHelpResource = cs;

            cs.AddSCListenerExt<SCResponse_Clan>((csMessage, scMessage, timeout, success) =>
            {
                if (csMessage == _csReceiveHelpResource)
                {
                    if (success && scMessage != null)
                    {
                        if (scMessage.success)
                        {
                            _popup?.IncreaseSuccess(_index);
                            Center.GetOrCreate<ClanService>().AddHelpResourceSuccess();
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

                    _csReceiveHelpResource = null;
                }
            }, int.MaxValue).Send();
        }
    }
}
