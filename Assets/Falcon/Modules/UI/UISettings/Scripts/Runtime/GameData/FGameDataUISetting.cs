/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
*/

using Falcon.Modules.Core.AccountData;

namespace Falcon.Modules.UI.Settings.Runtime
{
    [FGameDataType("ui_setting")]
    public class FGameDataUISetting : FGameData<FGameDataUISetting>
    {
        public int sound = 1;
        public int vibrate = 1;
        public int music = 1;
        public string languageCode = "en";
    }
}
