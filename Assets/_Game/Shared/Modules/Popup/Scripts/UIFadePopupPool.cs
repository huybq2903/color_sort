/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-26
 */

using Falcon.Helpers.EventBus;
using Falcon.Shared.Addressable;
using UnityEngine;
using UnityEngine.Pool;

namespace Falcon.Shared.EasyPopup
{
    public class UIFadePopupPool : MonoBehaviour
    {
        public const string REQUEST_GET_FADE = "falcon.game.get_fade_popup";
        public const string EVENT_RETURN_FADE = "falcon.game.return_fade_popup";

        private GameObject fadePrefab;
        private ObjectPool<GameObject> pool;
        private void Awake()
        {
            fadePrefab = AddressableExtensions.Load<GameObject>("UIFade");
            pool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(fadePrefab),
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: Destroy
            );
        }

        private void OnDestroy()
        {
            pool = null;
        }

        private void OnEnable()
        {
            GameRequest<GameObject>.Register(REQUEST_GET_FADE, TryGetFade, this);
            GameEvent<GameObject>.Register(EVENT_RETURN_FADE, ReturnFade, this);
        }

        private void OnDisable()
        {
            GameRequest<GameObject>.Unregister(REQUEST_GET_FADE);
            GameEvent<GameObject>.Unregister(EVENT_RETURN_FADE, ReturnFade, this);
        }

        private GameObject TryGetFade() => pool?.Get();

        private void ReturnFade(GameObject fade)
        {
            if (!fade) return;
            fade.transform.SetParent(transform);
            pool.Release(fade);
        }
    }
}