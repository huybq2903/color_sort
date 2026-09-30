using Falcon.Shared.Tab;
using UnityEngine;

namespace Game.Shared.Clan
{
    internal enum NoClanPanelTab
    {
        ListClan,
        SearchClan,
        CreateClan,
    }

    internal class NoClanPanel : MonoBehaviour
    {
        [SerializeField] private UITab_Parent _tabs;

        public void SelectTab(NoClanPanelTab type) => _tabs.Active((int)type);
    }
}
