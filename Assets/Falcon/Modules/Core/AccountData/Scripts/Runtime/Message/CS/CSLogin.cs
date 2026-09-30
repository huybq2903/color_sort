/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.AccountData 
{
    [FAMessage("cs_login")]
    public class CSLogin : CSMessage
    {
        public int code;
        public string token;
        public string device_id;
        public int sequence;
        public int app_version_int;
        public string platform;
        public string fbId;
        public string googleId;
        public string appleId;
    
        public CSLogin(int code, string token, string device_id, int sequence, int appVersion, string platform, string fbId, string googleId, string appleId)
        {
            this.code = code;
            this.token = token;
            this.device_id = device_id;
            this.sequence = sequence;
            this.app_version_int = appVersion;
            this.platform = platform;
            this.fbId = fbId;
            this.googleId = googleId;
            this.appleId = appleId;
        }

    }
}
