using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Game.Shared.Clan
{
    public class ClickOutside : MonoBehaviour, IDeselectHandler
    {
        [Header("Event")]
        public UnityEvent onClickOutside;

        public void OnDeselect(BaseEventData eventData)
        {
            // Đừng quyết ngay lập tức; đợi EventSystem “ổn định” xong.
            StartCoroutine(CheckAfterFrame());
        }

        IEnumerator CheckAfterFrame()
        {
            // chờ tới frame sau (có thể dùng WaitForEndOfFrame cũng được)
            yield return null;

            var es = EventSystem.current;
            var newSel = es ? es.currentSelectedGameObject : null;

            // Nếu selection mới vẫn là chính nó hoặc con của nó → coi như click "bên trong" => bỏ qua
            if (newSel && (newSel == gameObject || newSel.transform.IsChildOf(transform)))
                yield break;

            // Ngược lại, thực sự đã mất focus ra ngoài
            onClickOutside?.Invoke();
        }
    }
}
