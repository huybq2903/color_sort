using TMPro;
using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class ClanChatRoom_PlayerComponentInMessage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameTxt;
        [SerializeField] private Transform _avatarUIPos;
        [SerializeField] private Transform _namestyleUIPos;

        private void Awake()
        {
            if (_nameTxt != null) _nameTxt.overflowMode = TextOverflowModes.Ellipsis;
        }

        public void InitPlayerData(int sender_code, string sender_name, string profileData, string bonus_data)
        {
            if (_nameTxt != null) _nameTxt.text = sender_name;

            var data = new ClanMemberInfo
            {
                code = sender_code,
                profileData = profileData,
                bonusData = bonus_data
            };

            if (_avatarUIPos != null && _avatarUIPos.childCount > 0)
            {
                var avtUIObj = _avatarUIPos.GetChild(0).GetChild(0).gameObject;
                Center.GetOrCreate<ClanService>().OnSetAvatarUI(avtUIObj, data);
            }
            if (_namestyleUIPos != null && _namestyleUIPos.childCount > 0)
            {
                var nameStyleUIObj = _namestyleUIPos.GetChild(0).GetChild(0).gameObject;
                Center.GetOrCreate<ClanService>().OnSetNameStyleUI(_nameTxt.gameObject, _namestyleUIPos.gameObject, nameStyleUIObj, data);
            }
        }
    }
}
