
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
    public abstract class TranslateTextBase : MonoBehaviour
    {
        public event Action OnTextChangedEvent;
        private string beforeText = "";
        
        public abstract string text
        {
            get;
        }

        public abstract float alpha { get; set; }

        public abstract Component OriginTextComponent { get; }
        public string BeforeText { get => beforeText; set => beforeText = value; }

        public abstract void GetOriginalTextDirty();

        public abstract Text CreateDuplicatedText();

        public abstract void RefreshTranslatedTextProps(Text translatedText);
    }
}
