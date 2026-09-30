/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-07-18


using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.PhoneUtil.Runtime
{
    [FAMessage(("cs_capture_screenshot_rsp"))]
    public class CSCaptureScreenshotRsp : CSMessage
    {
        public byte[] screenshotData;
        
        public CSCaptureScreenshotRsp(byte[] screenshotData)
        {
            this.screenshotData = screenshotData;
        }
    }
}