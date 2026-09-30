/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-10
*/

using System.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.UI.Runtime;
using I2.Loc;
using UnityEngine;

namespace Falcon.Modules.UI.Settings.Runtime
{
    public static class UISettingWrapper
    {
        [RuntimeInitializeOnLoadMethod]
        internal static async void Init()
        {
            await Task.Yield();
            //Force Set Language Code
            GameEvent<string>.Emit("falcon.modules.ui.settings_language_save", LocalizationManager.CurrentLanguageCode);
        }
    }
}
