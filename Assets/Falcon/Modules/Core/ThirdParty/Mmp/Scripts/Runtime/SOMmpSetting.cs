/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-19
 */

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mmp.Runtime
{
    public class SOMmpSetting : ScriptableObject
    {
        [ValueDropdown(nameof(AppsflyerOnly))] [Title("Network Selector")]
        public MmpType mmp = MmpType.Appsflyer;

        private IEnumerable<MmpType> AppsflyerOnly => new[] { MmpType.Appsflyer };
        private bool IsAppsflyer => mmp == MmpType.Appsflyer;
        private bool IsAdjust => mmp == MmpType.Adjust;

        #region appsflyer

        [FoldoutGroup("Settings Appsflyer", expanded: true), PropertyOrder(0)]
        [BoxGroup("Settings Appsflyer/Dev Key")]
        [ShowIf(nameof(IsAppsflyer))]
        [HideLabel]
        public string devKey;

        [BoxGroup("Settings Appsflyer/App Id (only IOS)"), PropertyOrder(1)] [ShowIf(nameof(IsAppsflyer))] [HideLabel]
        public string appId;

        #endregion

        #region adjust

        [FoldoutGroup("Settings Adjust", expanded: true), PropertyOrder(0)]
        [BoxGroup("Settings Adjust/App Token")]
        [ShowIf(nameof(IsAdjust))]
        [HideLabel]
        public string appToken;

        [FoldoutGroup("Settings Adjust/Event Token"), PropertyOrder(1)]
        [LabelText("  Event Interstitial Show Token")]
        [ShowIf(nameof(IsAdjust))]
        public string interstitialShowToken;

        [FoldoutGroup("Settings Adjust/Event Token"), PropertyOrder(2)]
        [LabelText("  Event Interstitial Displayed Token")]
        [ShowIf(nameof(IsAdjust))]
        public string interstitialDisplayedToken;

        [FoldoutGroup("Settings Adjust/Event Token"), PropertyOrder(3)]
        [LabelText("  Event Rewarded Show Token")]
        [ShowIf(nameof(IsAdjust))]
        public string rewardedShowToken;

        [FoldoutGroup("Settings Adjust/Event Token"), PropertyOrder(4)]
        [LabelText("  Event Rewarded Displayed Token")]
        [ShowIf(nameof(IsAdjust))]
        public string rewardedDisplayedToken;

        [FoldoutGroup("Settings Adjust/Event Token"), PropertyOrder(4)]
        [LabelText("  IAP Token")]
        [ShowIf(nameof(IsAdjust))]
        public string inAppPurchaseToken;

        #endregion
    }

    public enum MmpType
    {
        Appsflyer,
        Adjust
    }
}