using Falcon.Helpers.EventBus;
using Falcon.Modules.UI.Menu.Runtime;
using Falcon.Shared.RewardFlow;
using UnityEngine;

public class MenuTabAutoAddResourceUI : MonoBehaviour
{
    public string[] resourceNames;
    private MenuTabAnimator[] lsTabs;

    private void OnEnable()
    {
        GameEvent.Register(MenuConst.EVENT_MENU_LOAD_FEATURES_COMPLETE, OnLoadFeaturesComplete, this);
    }

    private void OnDisable()
    {
        GameEvent.Unregister(MenuConst.EVENT_MENU_LOAD_FEATURES_COMPLETE, OnLoadFeaturesComplete, this);
    }

    private void OnLoadFeaturesComplete()
    {
        lsTabs = GetComponentsInChildren<MenuTabAnimator>(true);
        var indexMid = lsTabs.Length / 2;
        foreach (var s in resourceNames)
        {
            var rs = lsTabs[indexMid].gameObject.AddComponent<UIResource>();
            rs.resourceName = s;
            rs.where = string.Empty;
            rs.icon = lsTabs[indexMid].icon;
            UIResourceManager.AddResource(rs);
        }
    }
}