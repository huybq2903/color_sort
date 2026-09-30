/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
*/

using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Modules.UI.Menu.Runtime
{
    public class MenuNavigatorConfig : ScriptableObject
    {
        [InfoBox("Số lượng tab tối đa")]
        public int maxNumberTab = 3;

        [InfoBox("Tên và hình đại diện của tab")]
        public List<string> termI2NameTabs;
        public List<Sprite> sprIconActives;
        public List<Sprite> sprIconDeactives;

        [InfoBox("Các thông số điều chỉnh kích thước và tween")]
        [Title("Props")]
        public float heightViewport = 300f;
        public float heightScrollBarSelected = 350f;
        public float scaleIconDeactive = 1f;
        public float scaleIconActive = 1.375f;
        public float anchorPosYIconDeactive = 0f;
        public float anchorPosYIconActive = 75f;
        public float scaleText = 1f;
        public float anchorPosYText = -72.5f;

        [Title("Tween 1")]
        public float durationScaleIconDeactive = 0.175f;
        public Ease easeScaleIconDeactive = Ease.InOutSine;

        [Title("Tween 2")]
        public float durationScaleIconActive = 0.275f;
        public Ease easeScaleIconActive = Ease.OutBack;

        [Title("Tween 3")]
        public float durationMoveIcon = 0.175f;
        public Ease easeMoveIcon = Ease.InOutSine;

        [Title("Tween 4")]
        public float durationScaleText = 0.25f;
        public Ease easeScaleText = Ease.OutBack;
    }
}
