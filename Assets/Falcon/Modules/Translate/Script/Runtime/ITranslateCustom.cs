
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-24
 */

using UnityEngine;

namespace Falcon.Modules.Translate.Runtime
{
    public interface ITranslateCustom
    {
        // Địa chỉ của prefab button của bạn, để Resources.Load ra 
        string ResourcesPathOfYourTranslateButton();
    }
}
