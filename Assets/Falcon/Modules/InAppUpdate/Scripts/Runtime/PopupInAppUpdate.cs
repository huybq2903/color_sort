/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-28
 */

using System;
using System.Threading.Tasks;
using Falcon.Helpers.Addressable;
using Falcon.Modules.Core.RemoteConfig;
using Falcon.Modules.LocalizationService.Runtime;
using UnityEngine;

namespace Falcon.Modules.InAppUpdate.Scripts.Runtime
{
    public class PopupInAppUpdate : MonoBehaviour
    {
        private enum UpdateType
        {
            None,
            Optional,
            Force
        }

        private const string _ADDRESSABLE_PATH = "PopupInAppUpdate";
        private const string _FORCE_TERM = "InAppUpdate/in_app_update_force_description";
        private const string _OPTIONAL_TERM = "InAppUpdate/in_app_update_description";

#if UNITY_IOS
        [SerializeField] private string _iosAppID = "6758605869";
#endif
        [SerializeField] private GameObject _container, _btnClose;
        [SerializeField] private AutoI2TermBinder _txtDescription;

        private static bool _alreadyCheck;
        private static bool _isProcessing;

        private static PopupInAppUpdate _popup;
        private UpdateType _updateType;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            AddressableHelper.PreloadPrefab(_ADDRESSABLE_PATH);
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public static void ShowUpdatePopupIfNeeded()
        {
            _ = ShowUpdatePopupIfNeededAsync();
        }

        public static async Task ShowUpdatePopupIfNeededAsync()
        {
            if (_alreadyCheck || _isProcessing) return;

            var updateType = GetUpdateType();

            if (updateType == UpdateType.None)
            {
                return;
            }

            _isProcessing = true;

            try
            {
                _popup = await AddressableHelper.LoadSharedPrefab<PopupInAppUpdate>(_ADDRESSABLE_PATH);

                if (_popup == null)
                {
                    _isProcessing = false;
                    return;
                }

                _alreadyCheck = true;
                _popup.Show(updateType);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private static UpdateType GetUpdateType()
        {
            var config = FConfigController.Instance.Config<InAppUpdateConfig>().force_and_optional_versions;
            if (string.IsNullOrEmpty(config)) return UpdateType.None;

            var versions = config.Split(";");
            if (versions.Length < 2) return UpdateType.None;

            var forceVer = versions[0].Trim();
            var optionalVer = versions[1].Trim();

            var ic = InAppUpdateHelper.CompareLower(Application.version, forceVer);
            if (ic != null && ic.Value) return UpdateType.Force;

            var fc = InAppUpdateHelper.CompareLower(Application.version, optionalVer);
            if (fc != null && fc.Value) return UpdateType.Optional;

            return UpdateType.None;
        }

        private void Show(UpdateType updateType)
        {
            _updateType = updateType;
            _container.SetActive(true);
            _btnClose.SetActive(updateType == UpdateType.Optional);

            var desTerm = updateType == UpdateType.Force ? _FORCE_TERM : _OPTIONAL_TERM;
            _txtDescription.SetTerm(desTerm);
        }

        public void ClickBtnUpdate()
        {
            if (_updateType == UpdateType.Optional) ClosePopup();
            OpenStore();
        }

        private void ClosePopup()
        {
            gameObject.SetActive(false);
            AddressableHelper.Release(_ADDRESSABLE_PATH);
        }

        public void ClickBtnClose()
        {
            ClosePopup();
        }

        private void OpenStore()
        {
#if UNITY_IOS
            if (string.IsNullOrEmpty(_iosAppID)) return;
            string url = $"itms-apps://itunes.apple.com/app/id{_iosAppID}";
            Application.OpenURL(url);
#elif UNITY_ANDROID
            string packageName = Application.identifier;
            string url = $"market://details?id={packageName}";
            try
            {
                Application.OpenURL(url);
            }
            catch (Exception)
            {
                string webUrl = $"https://play.google.com/store/apps/details?id={packageName}";
                Application.OpenURL(webUrl);
            }
#endif
        }
    }
}