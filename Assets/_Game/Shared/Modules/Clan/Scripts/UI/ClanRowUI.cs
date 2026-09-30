using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class ClanRowUI : MonoBehaviour
    {
        [SerializeField] private ClanLogoUI _logo;
        [SerializeField] private TextMeshProUGUI _txtName;
        [SerializeField] private TextMeshProUGUI _txtDescription;
        [SerializeField] private TextMeshProUGUI _txtMembers;

        private int _clanCode;

        private void Awake()
        {
            if (_txtName != null) _txtName.overflowMode = TextOverflowModes.Ellipsis;
            if (_txtDescription != null) _txtDescription.overflowMode = TextOverflowModes.Ellipsis;
        }

        public void Init(ClanDataShort data)
        {
            if (data == null) return;

            _clanCode = data.code;

            if (_logo != null) _logo.Init(data.avatar_id);
            if (_txtName != null) _txtName.text = data.name ?? string.Empty;
            if (_txtMembers != null) _txtMembers.text = $"{data.num_member}/{data.max_member}";
            if (_txtDescription != null) _txtDescription.text = data.description ?? string.Empty;
            SetActiveSafe(_txtDescription.gameObject, !string.IsNullOrWhiteSpace(data.description));
        }

        public void ViewTeamInfo()
        {
            Center.GetOrCreate<ClanService>().ShowClanInfoPopup(_clanCode, true);
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go == null) return;
            if (go.activeSelf != active) go.SetActive(active);
        }
    }
}
