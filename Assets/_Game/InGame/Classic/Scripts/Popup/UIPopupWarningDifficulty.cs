
using System;
using System.Collections;
using DG.Tweening;
using Falcon.Modules.Core.UI.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.InGame.UI
{
    public class UIPopupWarningDifficulty : MonoBehaviour
    {
        public Image imgBG;
        public Box hard;
        public Box superHard;

        [Serializable]
        public class Box
        {
            public RectTransform rectPanel;
            public RectTransform icon;
            public RectTransform text;
        }

        [Button]
        public virtual void UpdateUI(int difficulty)
        {
            imgBG.DOFade(0.9f, 0.5f).From(0).SetEase(Ease.InOutSine);
            hard.rectPanel.gameObject.SetActive(false);
            superHard.rectPanel.gameObject.SetActive(false);
            if (difficulty == 1)
            {
                hard.rectPanel.gameObject.SetActive(true);
                UpdateUI_TweenBox(hard);
            }
            else
            {
                superHard.rectPanel.gameObject.SetActive(true);
                UpdateUI_TweenBox(superHard);
            }
        }

        protected virtual void UpdateUI_TweenBox(Box box)
        {
            var panel = box.rectPanel;
            var icon = box.icon;
            var text = box.text;

            icon.gameObject.SetActive(false);
            text.gameObject.SetActive(false);

            StartCoroutine(IETween());
            IEnumerator IETween()
            {
                yield return panel.DOSizeDelta(new Vector2(panel.sizeDelta.x, 425f), 0.425f).From(new Vector2(panel.sizeDelta.x, 0)).SetEase(Ease.OutBack).WaitForCompletion();

                icon.gameObject.SetActive(true);
                yield return icon.transform.DOScale(1, 0.275f).From(0).SetEase(Ease.OutBack).WaitForCompletion();

                text.gameObject.SetActive(true);
                yield return text.transform.DOScale(1, 0.325f).From(0).SetEase(Ease.OutBack).WaitForCompletion();

                icon.transform.DOScale(1.075f, 0.25f).From(1).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                yield return new WaitForSeconds(1f);
                icon.transform.DOKill();

                imgBG.DOFade(0, 0.5f).SetEase(Ease.InOutSine);
                panel.DOSizeDelta(new Vector2(panel.sizeDelta.x, 0), 0.325f).WaitForCompletion();
                icon.transform.DOScale(0, 0.425f).SetEase(Ease.InBack);
                text.transform.DOScale(0, 0.5f).SetEase(Ease.InBack);

                yield return new WaitForSeconds(0.75f);

                UIWrapper.ClosePopup(transform);
            }
        }
    }
}
