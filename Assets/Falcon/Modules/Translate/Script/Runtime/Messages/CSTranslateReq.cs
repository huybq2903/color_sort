
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-24
 */

using System.Globalization;
using System.Text.RegularExpressions;
using Falcon;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Translate.Runtime
{
    [FAMessage("cs_translate_req")]
    public class CSTranslateReq : CSMessageWaitLoginSuccess
    {
        public string request_uuid;
        public string text = "";
        public string source_language = ""; 
        public string target_language = "";

        public CSTranslateReq()
        {
            request_uuid = System.Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="text">Text to be translated</param>
        public CSTranslateReq(string text)
        {
            request_uuid = System.Guid.NewGuid().ToString();
            this.text = GetRemoveRickTextTags(text);
            source_language = "";
            target_language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="text">Text to be translated</param>
        /// <param name="sourceLanguage">Source language</param>
        /// <param name="targetLanguage">Target language - Can be empty</param>
        public CSTranslateReq(string text, string sourceLanguage, string targetLanguage)
        {
            request_uuid = System.Guid.NewGuid().ToString();
            this.text = GetRemoveRickTextTags(text);
            source_language = sourceLanguage;
            target_language = targetLanguage;
        }

        static string GetRemoveRickTextTags(string input)
        {
            string pattern = @"<[^>]*>";
            string output = Regex.Replace(input, pattern, string.Empty);
            return output;
        }
    }
}