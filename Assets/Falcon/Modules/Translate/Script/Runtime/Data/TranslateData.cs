
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-24
 */

using System;

namespace Falcon.Modules.Translate.Runtime
{
    [Serializable]
    public class TranslateData
    {
        public string requestUuid;
        public string originText;
        public string translatedText;
    }
}