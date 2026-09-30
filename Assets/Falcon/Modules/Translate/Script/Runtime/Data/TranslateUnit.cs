
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-24
 */

using System;
using UnityEngine.UI;

namespace Falcon.Modules.Translate.Runtime
{
    [Serializable]
    public class TranslateUnit
    {
        //public TextMeshProUGUI originalTextTmp;
        public TranslateTextBase originalTextTmp;
        public Text translatedText;
        public TranslateStatus status;
        public TranslateData translateData;

        private bool textAvailable = false;
        private float defaultTmpAlpha = 1f;

        public TranslateUnit(TranslateTextBase originalTextTmp)
        {
            this.originalTextTmp = originalTextTmp;
            translateData = new TranslateData()
            {
                originText = originalTextTmp.text
            };
            status = TranslateStatus.Translate;
        }

        public bool HasChanged()
        {
            return originalTextTmp == null ||
                   (translateData != null && originalTextTmp.text != translateData.originText);
        }

        public void ResetData()
        {
            translateData = new TranslateData
            {
                originText = originalTextTmp != null ? originalTextTmp.text : string.Empty,
                translatedText = string.Empty,
                requestUuid = string.Empty
            };
            status = TranslateStatus.Translate;
            if (textAvailable) translatedText.gameObject.SetActive(false);
            if (originalTextTmp) originalTextTmp.alpha = defaultTmpAlpha;
        }

        public void SpawnText(string translateTextData)
        {
            if (!translatedText)
            {
                defaultTmpAlpha = originalTextTmp.alpha;
                translatedText = originalTextTmp.CreateDuplicatedText();
            }

            /*translatedText.rectTransform.ResizeToParentSize();
            translatedText.font = originalTextTmp.font.sourceFontFile;
            translatedText.color = originalTextTmp.color;
            translatedText.ConvertAlignment(originalTextTmp);
            translatedText.supportRichText = originalTextTmp.richText;
            /*translatedText.horizontalOverflow =
                originalTextTmp.enableWordWrapping || originalTextTmp.autoSizeTextContainer
                    ? HorizontalWrapMode.Wrap
                    : HorizontalWrapMode.Overflow;
            translatedText.verticalOverflow = VerticalWrapMode.Truncate;
            translatedText.fontSize = (int) originalTextTmp.fontSize;
            translatedText.resizeTextMinSize = (int) Mathf.Min(originalTextTmp.fontSize,
                Mathf.Max(20, originalTextTmp.fontSize * 0.5f));#1#
            translatedText.horizontalOverflow = HorizontalWrapMode.Wrap;
            translatedText.verticalOverflow = VerticalWrapMode.Truncate;
            translatedText.resizeTextMinSize = Mathf.Max(1, (int) (originalTextTmp.fontSize * 3f/5f));
            translatedText.resizeTextMaxSize = (int) originalTextTmp.fontSize;
            translatedText.resizeTextForBestFit = true;
            translatedText.raycastTarget = originalTextTmp.raycastTarget;
            translatedText.raycastPadding = originalTextTmp.raycastPadding;
            translateData.translatedText = translateTextData;*/

            originalTextTmp.RefreshTranslatedTextProps(translatedText);
            
            translatedText.gameObject.SetActive(true);
            originalTextTmp.alpha = 0f;
            translatedText.text = translateTextData;

            /*translatedText.SetGreatestFontSize(fitWidth: translatedText.horizontalOverflow !=
                                                         HorizontalWrapMode.Overflow);*/
            textAvailable = true;
            status = TranslateStatus.Revert;
        }

        public void Revert()
        {
            originalTextTmp.alpha = defaultTmpAlpha;
            if (textAvailable)
            {
                translatedText.gameObject.SetActive(false);
            }

            status = TranslateStatus.Translate;
        }

        public void StopTranslate()
        {
            status = TranslateStatus.Translate;
        }
        
        public CSTranslateReq PrepareMessage()
        {
            if (originalTextTmp == null || string.IsNullOrWhiteSpace(originalTextTmp.text))
            {
                return null;
            }

            translateData.originText = originalTextTmp.text;
            CSTranslateReq cs = new CSTranslateReq(translateData.originText);
            return cs;
        }

        /// <summary>
        /// Unit needs to be translated or not.
        /// </summary>
        /// <returns></returns>
        public bool NeedTranslate()
        {
            if (originalTextTmp == null || string.IsNullOrWhiteSpace(originalTextTmp.text) ||
                (!string.IsNullOrWhiteSpace(translateData.translatedText) &&
                 string.Equals(translateData.originText, originalTextTmp.text)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Translate unit depends on the current translation data
        /// </summary>
        public void TranslateDependOnData()
        {
            if (!textAvailable) SpawnText(translateData.translatedText);
            else
            {
                translatedText.gameObject.SetActive(true);
                translatedText.text = translateData.translatedText;
            }
            originalTextTmp.alpha = 0f;
            status = TranslateStatus.Revert;
        }
    }
}