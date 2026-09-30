/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-18
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace Falcon.Modules.UnityLocalization.Runtime
{
    [Serializable]
    public class LocalizedTMP_Font : LocalizedAsset<TMP_FontAsset> {}
    
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizeTMP_FontComponent : LocalizedAssetBehaviour<TMP_FontAsset, LocalizedTMP_Font>
    {
        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        protected override void UpdateAsset(TMP_FontAsset localizedAsset)
        {
            _text.font = localizedAsset;
        }
    }
}