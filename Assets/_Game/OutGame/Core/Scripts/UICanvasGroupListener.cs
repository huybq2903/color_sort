/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-11
 */

using Falcon.Helpers.EventBus;
using UnityEngine;

namespace Falcon.Shared.Common
{
    public class UICanvasGroupListener : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            GameEvent<float>.Register(GameKeys.SET_ALPHA_HUD, SetAlphaHud, this);
        }

        private void OnDisable()
        {
            GameEvent<float>.Unregister(GameKeys.SET_ALPHA_HUD, SetAlphaHud, this);
        }

        private void SetAlphaHud(float alpha)
        {
            _canvasGroup.alpha = alpha;
        }
    }
}