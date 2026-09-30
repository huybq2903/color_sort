
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-24
 */

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.Translate.Runtime
{
    [RequireComponent(typeof(Text))]
    public class TranslateTextLegacy : TranslateTextBase
    {
        [SerializeField] private Text originalText;
        Font font;

        public override void GetOriginalTextDirty()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (originalText == null) originalText = GetComponent<Text>();
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
            get => originalText.color.a;
            set => originalText.color =
                new Color(originalText.color.r, originalText.color.g, originalText.color.b, value);
        }

        public override Component OriginTextComponent => originalText;

        public override Text CreateDuplicatedText()
        {

            //var translatedText = new GameObject(originalText.gameObject.name.Replace(" (TMP)", "") + " (Translated)")
            //    .AddComponent<Text>();

            //translatedText.transform.SetParent(originalText.transform, false);
            var translatedText = Instantiate(originalText, originalText.transform.parent);
            translatedText.transform.SetSiblingIndex(originalText.transform.GetSiblingIndex() + 1);

            return translatedText;
        }

        public override void RefreshTranslatedTextProps(Text translatedText)
        {
            return;
            // Sao chép tất cả các thuộc tính từ originalText sang newText
            translatedText.font = originalText.font; // font
            translatedText.text = originalText.text;
            translatedText.fontSize = originalText.fontSize;
            translatedText.alignment = originalText.alignment;
            translatedText.color = originalText.color;
            translatedText.material = originalText.material;
            translatedText.raycastTarget = originalText.raycastTarget;
            translatedText.lineSpacing = originalText.lineSpacing;
            translatedText.supportRichText = originalText.supportRichText;
            
            /*translatedText.horizontalOverflow = originalText.horizontalOverflow;
            translatedText.verticalOverflow = originalText.verticalOverflow;
            translatedText.resizeTextForBestFit = originalText.resizeTextForBestFit;
            translatedText.resizeTextMinSize = originalText.resizeTextMinSize;
            translatedText.resizeTextMaxSize = originalText.resizeTextMaxSize;*/
            
            translatedText.horizontalOverflow = HorizontalWrapMode.Wrap;
            translatedText.verticalOverflow = VerticalWrapMode.Truncate;
            translatedText.resizeTextMinSize = Mathf.Max(1, (int) (originalText.fontSize * 3f / 5f));
            translatedText.resizeTextMaxSize = (int) originalText.fontSize;
            translatedText.resizeTextForBestFit = true;
            
            translatedText.alignment = originalText.alignment;
            translatedText.alignByGeometry = originalText.alignByGeometry;
            translatedText.fontStyle = originalText.fontStyle;
            translatedText.maskable = originalText.maskable;

            // Sao chép RectTransform để đảm bảo Text mới có cùng kích thước và vị trí
            translatedText.rectTransform.ResizeToParentSize();
        }

#if UNITY_EDITOR
        private void Reset()
        {
            originalText = GetComponent<Text>();
        }
#endif
    }
}
