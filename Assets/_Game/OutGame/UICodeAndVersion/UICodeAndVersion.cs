// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-05-21

using Falcon.Modules.Core.AccountData;
using TMPro;
using UnityEngine;

namespace Falcon.OutGame.Core
{
    public class UICodeAndVersion : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        private void Awake()
        {
            SetUI();
        }

        private void OnEnable()
        {
            AccountManager.Instance.OnUpdateFromServer += Callback;
        }

        private void OnDisable()
        {
            AccountManager.Instance.OnUpdateFromServer += Callback;
        }

        private void Callback(ClientData clientData) => SetUI();

        private void SetUI()
        {
            text.SetText($"{AccountManager.Instance.Code} v{Application.version}");
        }
    }
}