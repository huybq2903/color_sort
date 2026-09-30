using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class UIClanPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _noClanPanel;
        [SerializeField] private GameObject _inClanPanel;
        [SerializeField] private GameObject _lockPanel;
        [SerializeField] private GameObject _noInternetPanel;

        private void OnEnable()
        {
            Center.GetOrCreate<ClanService>().OnClanChanged += OnMyClanChanged;
            Center.GetOrCreate<ClanService>().OnConnectionChanged += OnOpenOrReconnect;
            OnOpenOrReconnect();
        }

        private void OnDisable()
        {
            Center.GetOrCreate<ClanService>().OnClanChanged -= OnMyClanChanged;
            Center.GetOrCreate<ClanService>().OnConnectionChanged -= OnOpenOrReconnect;
        }

        private void OnOpenOrReconnect()
        {
            DisableAll();
            if (!Center.GetOrCreate<ClanService>().Unlocked) { SetActiveSafe(_lockPanel, true); return; }
            if (!Center.GetOrCreate<ClanService>().Connected) { SetActiveSafe(_noInternetPanel, true); return; }
            SetActiveSafe(_noClanPanel, Center.GetOrCreate<ClanService>().MyClanCode == 0);
            SetActiveSafe(_inClanPanel, Center.GetOrCreate<ClanService>().MyClanCode > 0);
        }

        // Gop onJoinClanAction+onLeaveClanAction cu, doc MyClanCode de biet chieu
        private void OnMyClanChanged()
        {
            if (!gameObject.activeInHierarchy) return;
            bool inClanNow = Center.GetOrCreate<ClanService>().MyClanCode > 0;
            SetActiveSafe(_noClanPanel, !inClanNow);
            SetActiveSafe(_inClanPanel, inClanNow);
        }

        private void DisableAll()
        {
            SetActiveSafe(_lockPanel, false);
            SetActiveSafe(_inClanPanel, false);
            SetActiveSafe(_noClanPanel, false);
            SetActiveSafe(_noInternetPanel, false);
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go == null) return;
            if (go.activeSelf != active) go.SetActive(active);
        }
    }
}
