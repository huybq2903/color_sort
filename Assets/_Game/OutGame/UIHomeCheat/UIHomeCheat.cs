using BayatGames.SaveGamePro;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Modules.Level.Core;
using Falcon.Shared.Common;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Falcon.OutGame.Core
{
    public class UIHomeCheat : MonoBehaviour
    {
        [SerializeField] private Button btnSetGold, btnSetLevel, btnToggle, btnRefreshHome, btnResetPlayer;
        [SerializeField] private GameObject goContent;
        [SerializeField] private TMP_InputField inputText;
        [SerializeField] private TMP_Text txtBtnToggle;

        private void Awake()
        {
            btnSetGold.onClick.AddListener(OnGuiGoldInputOk);
            btnSetLevel.onClick.AddListener(OnGuiInputOk);
            btnToggle.onClick.AddListener(OnToggleClick);
            btnRefreshHome.onClick.AddListener(OnRefreshHome);
            if (btnResetPlayer) btnResetPlayer.onClick.AddListener(OnResetPlayer);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            return;
#endif
            Destroy(gameObject);
        }

        private void OnRefreshHome()
        {
            SceneManager.LoadScene("HomeScene");
        }

        private void OnToggleClick()
        {
            txtBtnToggle.text = goContent.activeSelf ? "▼" : "►";
            goContent.SetActive(!goContent.activeSelf);
        }

        private void OnGuiInputOk()
        {
            if (int.TryParse(inputText.text, out var level))
            {
                LevelData.Instance.level = level;
                LevelData.Instance.Save();
                GameEvent.Emit(GameKeys.PLAY_LEVEL);
            }
        }

        private void OnGuiGoldInputOk()
        {
            if (int.TryParse(inputText.text, out var gold))
            {
                var goldResource = ResourceCollector.Instance.GetResourceInCollector("gold");
                var currentGold = (int)goldResource.Get;
                var difference = gold - currentGold;

                if (difference > 0)
                {
                    ResourceCollector.Instance.ResourceAdd("gold", difference, null, "Debug Set Gold");
                }
                else if (difference < 0)
                {
                    ResourceCollector.Instance.ResourceRemove("gold", -difference, null, "Debug Set Gold");
                }

                ResourceCollector.Instance.SaveAndUpdateServerOfResourceData("gold");
            }
        }

        private void OnResetPlayer()
        {
            var clientData = AccountManager.Instance.ClientData;
            var allTypeNames = FGameDataRegistry.Instance.GetAllTypeNames();
            foreach (var typeName in allTypeNames)
            {
                clientData.gameDatas[typeName] = FGameDataRegistry.Instance.GetGameDataInstance(typeName);
            }

            // Gán lại để trigger setter (lưu local + cập nhật internal state)
            AccountManager.Instance.ClientData = clientData;
            AccountManager.Instance.UpdateToServer();
            SaveGame.Clear();
            SceneManager.LoadScene("HomeScene");
        }
    }
}