using Falcon.Helpers.EventBus;
using Falcon.Modules.ChatRoom.Runtime;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class ClanChatRoomMessage_HELP : ChatRoomMessageUIBase
    {
        [SerializeField] private ClanChatRoom_PlayerComponentInMessage _playerComponent;
        [SerializeField] private Slider _slider;
        [SerializeField] private Button _helpBtn;
        [SerializeField] private TextMeshProUGUI _numAndMaxHelpsTmp;

        private bool _canClick = true;

        public override void Init(ChatRoomMessageData data)
        {
            Data = data;
            _playerComponent?.InitPlayerData(data.sender_code, data.sender_name, data.profileData, data.bonus_data);
            SetHelpData(data.message_content);
        }

        public override void UpdateMessage(ChatRoomMessageData newData) => Init(newData);

        public override void InteractMessage(string interact_content) { }

        public void HelpOnClick()
        {
            if (!_canClick)
            {
                Center.GetOrCreate<ClanService>().ShowToastWaitAMomentAfterOnClick();
                return;
            }
            if (_chatbox == null || Data == null) return;

            _canClick = false;

            Vector3 helpBtnPos = _helpBtn.transform.position; // luu vi tri truoc khi bi don rac
            ChatRoomManager.InteractMessage(_chatbox.room_type, _chatbox.room_id, Data.message_uuid, string.Empty,
                onSuccessAction: () =>
                {
                    Center.GetOrCreate<ClanService>().GetRewardAfterHelpSuccess();
                    GameEvent<(int, Vector3?, string)>.Emit(GameKeys.REWARD_ENTRY_SPAWN_GOLD,
                        (Center.GetOrCreate<ClanService>().CoinReceivePerHelp, helpBtnPos, "clan_help"));
                    Debug.Log("Help Success!");
                },
                onFailAction: (message) =>
                {
                    Center.GetOrCreate<ClanService>().ShowToast(message);
                    Debug.Log("Help Failed: " + message);
                },
                onTimeoutAction: () =>
                {
                    Debug.Log("Help Timeout!");
                },
                onDoneAction: () =>
                {
                    _canClick = true;
                }
            );
        }

        private void SetHelpData(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            ClanHelpData helpData = null;
            try { helpData = JsonUtility.FromJson<ClanHelpData>(json); }
            catch (Exception e) { Debug.LogWarning($"HELP parse failed: {e.Message}"); }
            if (helpData == null) return;

            if (_numAndMaxHelpsTmp != null)
                _numAndMaxHelpsTmp.text = $"{helpData.num_helps}/{helpData.max_helps}";

            if (_slider != null)
                _slider.value = helpData.max_helps > 0 ? (float)helpData.num_helps / helpData.max_helps : 0f;

            if (_helpBtn != null)
                _helpBtn.interactable = (helpData.num_helps < helpData.max_helps && helpData.helpable);
        }
    }

    [Serializable]
    public class ClanHelpData
    {
        public int num_helps;
        public int max_helps;
        public bool helpable;
    }
}
