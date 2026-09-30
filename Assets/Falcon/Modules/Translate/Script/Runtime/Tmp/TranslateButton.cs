
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-24
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.Translate.Runtime
{
    public class TranslateButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        
        [SerializeField] private GameObject translateGo;
        [SerializeField] private GameObject translatingGo;
        [SerializeField] private GameObject revertGo;

        public Button Button => button;
        
        public void InitStatus( TranslateStatus status)
        {
            HideTranslateGo();
            HideTranslatingGo();
            HideRevertGo();
            
            ShowButton(status);
        }
        
        public void OnStatusChanged(TranslateStatus oldStatus, TranslateStatus newStatus)
        {
            HideButton(oldStatus);
            ShowButton(newStatus);
        }

        void ShowButton(TranslateStatus newStatus)
        {
            switch (newStatus)
            {
                case TranslateStatus.Translate:
                    ShowTranslateGo();
                    break;
                case TranslateStatus.Translating:
                    ShowTranslatingGo();
                    break;
                case TranslateStatus.Revert:
                    ShowRevertGo();
                    break;
            }
        }

        void HideButton(TranslateStatus oldStatus)
        {
            switch (oldStatus)
            {
                case TranslateStatus.Translate:
                    HideTranslateGo();
                    break;
                case TranslateStatus.Translating:
                    HideTranslatingGo();
                    break;
                case TranslateStatus.Revert:
                    HideRevertGo();
                    break;
            }
        }

        protected virtual void ShowTranslateGo()
        {
            translateGo.SetActive(true);
        }

        protected virtual void HideTranslateGo()
        {
            translateGo.SetActive(false);
        }

        protected virtual void ShowTranslatingGo()
        {
            translatingGo.SetActive(true);
        }

        protected virtual void HideTranslatingGo()
        {
            translatingGo.SetActive(false);
        }

        protected virtual void ShowRevertGo()
        {
            revertGo.SetActive(true);
        }

        protected virtual void HideRevertGo()
        {
            revertGo.SetActive(false);
        }
    }
}


