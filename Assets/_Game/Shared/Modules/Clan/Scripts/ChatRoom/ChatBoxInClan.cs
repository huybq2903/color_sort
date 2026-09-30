using Falcon.Modules.ChatRoom.Runtime;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Falcon.Shared.Common;
using Falcon.Shared.Common.Time;
namespace Game.Shared.Clan
{
    public class ChatBoxInClan : ChatBoxBase
    {
        private const string COOLDOWN_KEY = "clan_help_cooldown";
        private const string COOLDOWN_REFRESH_KEY = "clan_help_cooldown_refresh";

        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private ClanCountdown _countdown;
        [SerializeField] private Button _requestBtn;

        [SerializeField] private bool _canChat = true;
        private Coroutine _refreshCo;
        private bool _firstFocusPassed;

        protected override void OnEnable()
        {
            base.OnEnable();

            RefreshTimeRequest();
            Center.GetOrCreate<ClanService>().OnChangeUserData += GetFirstPage;
            Center.GetOrCreate<ClanService>().OnGetHelpConfig += OnSCGetHelpConfig;

            if (_inputField != null)
            {
                _inputField.onSubmit.RemoveListener(OnSendChat);
                _inputField.onSubmit.AddListener(OnSendChat);
            }

            new CSGetHelpConfig().Send();
        }

        protected override void OnDisable()
        {
            Center.GetOrCreate<ClanService>().OnChangeUserData -= GetFirstPage;
            Center.GetOrCreate<ClanService>().OnGetHelpConfig -= OnSCGetHelpConfig;

            if (_inputField != null)
                _inputField.onSubmit.RemoveListener(OnSendChat);

            WrapperTime.RemoveTick(COOLDOWN_REFRESH_KEY);
            StopRefreshCoroutine();
            base.OnDisable();
        }

        protected override void OnNewMessage(SCNewMessage data)
        {
            base.OnNewMessage(data);
            if (data != null && data.message != null && data.message.sender_code == Center.GetOrCreate<ClanService>().YourPlayerCode())
                MoveToBottom();
        }

        public void InitChatRoomId(string id) => room_id = id;

        public void ChatButtonOnClick() => _inputField?.Select();

        public void RequestButtonOnClick()
        {
            if (Center.GetOrCreate<ClanService>().GetDateTimeNow() >= Center.GetOrCreate<ClanService>().TimeCanCreateRequest)
            {
                ChatRoomManager.CreateMessage(room_type, room_id, "HELP", string.Empty,
                    onSuccessAction: () =>
                    {
                        Debug.Log("Request success!");
                        Center.GetOrCreate<ClanService>().TimeCanCreateRequest = Center.GetOrCreate<ClanService>().GetDateTimeNow().AddMilliseconds(Center.GetOrCreate<ClanService>().CooldownPerRequest);
                        RefreshTimeRequest();
                    },
                    onFailAction: (msg) => { Center.GetOrCreate<ClanService>().ShowToast(msg); Debug.Log(msg); },
                    onTimeoutAction: () => { Center.GetOrCreate<ClanService>().ShowToastTimeout(); Debug.Log("Time out!"); },
                    onDoneAction: () => { }
                );
            }
            else
            {
                Center.GetOrCreate<ClanService>().ShowToastWaitAMomentAfterOnClick();
            }
        }

        private void OnSendChat(string input)
        {
            if (!_canChat) return;
            _canChat = false;

            ChatRoomManager.CreateMessage(room_type, room_id, "TEXT", input,
                onSuccessAction: () => Debug.Log("Send success!"),
                onFailAction: (message) => { Debug.Log(message); Center.GetOrCreate<ClanService>().ShowToast(message); },
                onTimeoutAction: () => Debug.Log("Time out!"),
                onDoneAction: () => { _canChat = true; },
                timeOut: 2);
        }

        private void OnSCGetHelpConfig(SCGetHelpConfig data) => RefreshTimeRequest();

        private void RefreshTimeRequest()
        {
            if (_countdown == null || _requestBtn == null) return;

            System.DateTime now = Center.GetOrCreate<ClanService>().GetDateTimeNow();
            System.DateTime endTime = Center.GetOrCreate<ClanService>().TimeCanCreateRequest;
            bool waiting = now < endTime;

            _requestBtn.interactable = !waiting;

            if (waiting)
            {
                long endSecond = WrapperTime.CurrentSecond + (long)(endTime - now).TotalSeconds;
                _countdown.Begin(COOLDOWN_KEY, endSecond);
                // Key riêng vì actionEnd của WrapperTime là single-cast, dùng chung sẽ đè mất OnEnd của ClanCountdown
                WrapperTime.AddTick(COOLDOWN_REFRESH_KEY, endSecond, null, StartRefreshCoroutine);
            }
            else
            {
                _countdown.Stop();
                WrapperTime.RemoveTick(COOLDOWN_REFRESH_KEY);
            }
        }

        private IEnumerator IEWaitAndRefreshTimeRequest()
        {
            yield return new WaitForSeconds(1f);
            RefreshTimeRequest();
            _refreshCo = null;
        }

        private void StartRefreshCoroutine()
        {
            StopRefreshCoroutine();
            _refreshCo = StartCoroutine(IEWaitAndRefreshTimeRequest());
        }

        private void StopRefreshCoroutine()
        {
            if (_refreshCo != null)
            {
                StopCoroutine(_refreshCo);
                _refreshCo = null;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _firstFocusPassed) RefreshTimeRequest();
            _firstFocusPassed = true;
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) RefreshTimeRequest();
        }
    }
}
