/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-07-18


using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.PhoneUtil.Runtime
{
    [FAMessage(("sc_capture_screenshot_req"))]
    public class SCCaptureScreenshotReq : SCMessage
    {
        public override void OnData()
        {
            global::PhoneUtil.Instance.CaptureScreenshotAsync((bytes =>
            {
                new CSCaptureScreenshotRsp(bytes).Compress().Send();
            }));
        }
    }
}