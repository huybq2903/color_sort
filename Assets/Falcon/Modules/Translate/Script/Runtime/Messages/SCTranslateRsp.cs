
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-24
 */

using Falcon.Modules.Core.Network;
using System;

namespace Falcon.Modules.Translate.Runtime
{
    [FAMessage("sc_translate_rsp")]
    public class SCTranslateRsp : SCMessage
    {
        public string request_uuid;
        public string text_original;
        public string text_translated;
        public string source_language;
        public string target_language;

        public static event Action<SCTranslateRsp> onGetDataEvent;

        public override void OnData()
        {
            onGetDataEvent?.Invoke(this);
            TranslateManager.OnGetTranslatedDataEvent(this);
        }
    }
}

