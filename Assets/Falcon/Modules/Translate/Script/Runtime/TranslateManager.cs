
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-24
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Falcon.Modules.Translate.Runtime
{
    public class TranslateManager
    {
        #region Don't care
        private static Dictionary<string, Action<string>> _translateActionDic = new Dictionary<string, Action<string>>();

        static ITranslateCustom _custom;

        public static void Register(ITranslateCustom custom)
        {
            if (_custom == null)
                _custom = custom;
        }

        public static string ResourcesPathOfYourTranslateButton()
        {
            if (_custom != null)
                return _custom.ResourcesPathOfYourTranslateButton();
            return "Prf/Translate Button";
        }

        private static void Translate(CSTranslateReq cs, Action<string> onTranslateCompleteAction)
        {
            if (_translateActionDic.ContainsKey(cs.request_uuid))
                _translateActionDic[cs.request_uuid] = onTranslateCompleteAction;
            else
                _translateActionDic.Add(cs.request_uuid, onTranslateCompleteAction);
            cs.Send();
        }

        internal static void OnGetTranslatedDataEvent(SCTranslateRsp sc)
        {
            if (_translateActionDic.ContainsKey(sc.request_uuid) == false || string.IsNullOrWhiteSpace(sc.text_translated)) return;

            _translateActionDic[sc.request_uuid]?.Invoke(sc.text_translated);
            _translateActionDic.Remove(sc.request_uuid);
        }
        #endregion

        #region For User
        /// <summary>
        /// Dịch một string, ngôn ngữ nguồn Server sẽ tự detect, ngôn ngữ đích là ngôn ngữ máy của bạn
        /// </summary>
        /// <param name="text"> string cần dịch </param>
        /// <param name="onTranslateCompleteAction"> Action được gọi khi dịch xong </param>
        public static void Translate(string text, Action<string> onTranslateCompleteAction)
        {
            var cs = new CSTranslateReq(text);
            Translate(cs, onTranslateCompleteAction);
        }

        /// <summary>
        /// Dịch một string. Thường sẽ dùng hàm trên thay vì hàm này.
        /// </summary>
        /// <param name="text"> string cần dịch </param>
        /// <param name="sourceLanguage"> ngôn ngữ nguồn </param>
        /// <param name="targetLanguage"> ngôn ngữ đích </param>
        /// <param name="onTranslateCompleteAction"> Action được gọi khi dịch xong </param>
        public static void Translate(string text, string sourceLanguage, string targetLanguage, Action<string> onTranslateCompleteAction)
        {
            var cs = new CSTranslateReq(text, sourceLanguage, targetLanguage);
            Translate(cs, onTranslateCompleteAction);
        }
        #endregion
    }
}
