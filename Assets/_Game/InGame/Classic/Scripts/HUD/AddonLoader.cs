// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-05-14

using System.Collections.Generic;
using Falcon.Helpers.UI;
using UnityEngine;

namespace Falcon.InGame.UI
{
    public class AddonLoader : MonoBehaviour
    {
        public List<string> featureAddons;

        protected virtual void Start()
        {
            for (var i = 0; i < featureAddons.Count; i++)
            {
                var a = i;
                var compAddon = gameObject.AddComponent<LoadPrefabFromAddressable>();
                compAddon.onLoadCompleted = () =>
                {
                    var prefabFeature = compAddon.Prefab;
                    prefabFeature.transform.SetSiblingIndex(a);
                };
                compAddon.Load(featureAddons[a]);
            }
        }
    }
}