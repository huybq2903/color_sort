using System.Collections.Generic;
using UnityEngine;

namespace Falcon.Shared.BaseBooster
{
    public class UIBoostersPanel : MonoBehaviour
    {
        private UIBoosterItem[] _items;

        private void Awake()
        {
            _items = GetComponentsInChildren<UIBoosterItem>(true);
            SortOrderItem();
        }

        private void SortOrderItem()
        {
            var data = new List<(Transform transform, int levelUnlock)>();
            foreach (var item in _items)
            {
                var type = item.boosterType;
                data.Add((item.transform, item.LevelUnlock));
            }

            data.Sort((a, b) => a.levelUnlock.CompareTo(b.levelUnlock));

            for (int i = 0; i < data.Count; i++)
                data[i].transform.SetSiblingIndex(i);
        }
    }
}