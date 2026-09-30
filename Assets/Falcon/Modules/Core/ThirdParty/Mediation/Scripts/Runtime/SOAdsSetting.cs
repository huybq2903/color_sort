/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public class SOAdsSetting : ScriptableObject
    {
        [FoldoutGroup("Settings Ads", expanded: true), PropertyOrder(0)]
        [BoxGroup("Settings Ads/Ad break time")]
        [ShowInInspector]
        [InlineProperty]
        [LabelText("  Time Break")]
        [InfoBox(
            "Thời gian hiện popup \"Ad break time\" trước khi hiện quảng cáo interstitial. Nếu giá trị = 0 thì sẽ ko hiện popup")]
        public float timeBreak = 1;
    }
}