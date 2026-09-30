
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-24
 */

using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Translate.Runtime
{
    [FAMessage("cs_translate_rsp")]
    public class CSTranslateRsp : CSMessageWaitLoginSuccess
    {
        public string request_uuid = "";
        public string text_original = "";
        public string text_translated = "";
        public string source_language = "";
        public string target_language = "";
        
        public CSTranslateRsp() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scTranslateReq">Request from Server</param>
        /// <param name="textTranslated">Translated text</param>
        /// <param name="sourceLanguage">Source language detected</param>
        public CSTranslateRsp(SCTranslateReq scTranslateReq, string textTranslated, string sourceLanguage)
        {
            request_uuid = scTranslateReq.request_uuid;
            text_original = scTranslateReq.text;
            text_translated = textTranslated;
            source_language = sourceLanguage;
            target_language = scTranslateReq.target_language;
        }
    }
}

