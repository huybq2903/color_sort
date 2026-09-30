/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */

using System;
using UnityEngine;

namespace Falcon.Shared.LoadingTransition
{
    [Serializable]
    public class IrisWipeConfig
    {
        [SerializeField] private Sprite _irisWipeBG;
        [SerializeField] private Sprite _irisWipeLogo;
        [SerializeField, Range(0.5f, 1f)] private float _wipeInDurationBG = 0.62f;
        [SerializeField, Range(0.2f, 1f)] private float _wipeOutDurationBG = 0.256f;
        [SerializeField, Range(0.25f, 0.5f)] private float _wipeInDurationLogo = 0.3f;
        [SerializeField, Range(0.25f, 0.5f)] private float _wipeOutDurationLogo = 0.35f;
        [SerializeField, Range(0, 1)] private float _wipeInDelayScaleLogo = 0.47f;
        [SerializeField] private AnimationCurve _logoScaleInCurve, _logoScaleOutCurve;
        
        public Sprite IrisWipeBG => _irisWipeBG;
        public Sprite IrisWipeLogo => _irisWipeLogo;
        public float WipeInDurationBG => _wipeInDurationBG;
        public float WipeOutDurationBG => _wipeOutDurationBG;
        public float WipeInDurationLogo => _wipeInDurationLogo;
        public float WipeOutDurationLogo => _wipeOutDurationLogo;
        public float WipeInDelayScaleLogo => _wipeInDelayScaleLogo;
        public AnimationCurve LogoScaleInCurve => _logoScaleInCurve;
        public AnimationCurve LogoScaleOutCurve => _logoScaleOutCurve;
    }
    
    [CreateAssetMenu(fileName = "UITransitionConfig",
        menuName = "Scriptable Objects/Falcon/Modules/UI/UITransition/Config")]
    public class UITransitionConfig : ScriptableObject
    {
        public const string SETTINGS_NAME = "UITransitionConfig";

        [Header("Progress Bar:"), Space(5)]
        [SerializeField] private float _animationSpeed = 3;
        
        [Header("Iris Wipe:"), Space(7)]
        [SerializeField] private IrisWipeConfig[] _irisWipeConfig;

        [Header("Fading:"), Space(10)]
        [SerializeField] private float _fadeDuration;
        
        public float AnimationSpeed => _animationSpeed;
        public float FadeDuration => _fadeDuration;
        public IrisWipeConfig[] IrisWipeConfig => _irisWipeConfig;
    }
}