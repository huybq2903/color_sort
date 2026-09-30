
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-24
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.Translate.Runtime
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TranslateTextTMP : TranslateTextBase
    {
        [SerializeField] private TextMeshProUGUI originalText;
        Font font;
        
        public override void GetOriginalTextDirty()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (originalText == null) originalText = GetComponent<TextMeshProUGUI>();
        }

        public override string text
        {
            get
            {
                GetOriginalTextDirty();
                return originalText.text;
            }
        }
        
        public override float alpha
        {
            get => originalText.alpha;
            set => originalText.alpha = value;
        }

        public override Component OriginTextComponent => originalText;

        public override Text CreateDuplicatedText()
        {
            var translatedText = new GameObject(originalText.gameObject.name.Replace(" (TMP)", "") + " (Translated)")
                .AddComponent<Text>();
            translatedText.transform.SetParent(originalText.transform);

            return translatedText;
        }

        public override void RefreshTranslatedTextProps(Text translatedText)
        {
            translatedText.rectTransform.ResizeToParentSize();
            translatedText.font = originalText.font.sourceFontFile; // font
            translatedText.color = originalText.color;
            translatedText.ConvertAlignment(originalText);
            translatedText.supportRichText = originalText.richText;
            /*translatedText.horizontalOverflow =
                originalTextTmp.enableWordWrapping || originalTextTmp.autoSizeTextContainer
                    ? HorizontalWrapMode.Wrap
                    : HorizontalWrapMode.Overflow;
            translatedText.verticalOverflow = VerticalWrapMode.Truncate;
            translatedText.fontSize = (int) originalTextTmp.fontSize;
            translatedText.resizeTextMinSize = (int) Mathf.Min(originalTextTmp.fontSize,
                Mathf.Max(20, originalTextTmp.fontSize * 0.5f));*/
            translatedText.horizontalOverflow = HorizontalWrapMode.Wrap;
            translatedText.verticalOverflow = VerticalWrapMode.Truncate;
            translatedText.resizeTextMinSize = Mathf.Max(1, (int) (originalText.fontSize * 3f / 5f));
            translatedText.resizeTextMaxSize = (int) originalText.fontSize;
            translatedText.resizeTextForBestFit = true;
            translatedText.raycastTarget = originalText.raycastTarget;
            translatedText.raycastPadding = originalText.raycastPadding;
        }
        
#if UNITY_EDITOR
        private void Reset()
        {
            originalText = GetComponent<TextMeshProUGUI>();
        }
#endif
    }
}
