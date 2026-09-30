/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-30
 */

using Falcon.Shared.BaseBooster;
using Falcon.Shared.Tutorial;
using UnityEngine.Scripting;

namespace Falcon.InGame.Booster
{
    [Preserve]
    [Tutorial("confirm_use_" + BoosterConst.BOOSTER_1)]
    public class TutorialConfirmUseBooster1 : ATutorialConfirmUseBooster { }

    [Preserve]
    [Tutorial("confirm_use_" + BoosterConst.BOOSTER_2)]
    public class TutorialConfirmUseBooster2 : ATutorialConfirmUseBooster { }

    [Preserve]
    [Tutorial("confirm_use_" + BoosterConst.BOOSTER_3)]
    public class TutorialConfirmUseBooster3 : ATutorialConfirmUseBooster { }

    [Preserve]
    [Tutorial("unlock_" + BoosterConst.BOOSTER_3)]
    public class TutorialUnlockBooster3 : ATutorialUnlockBooster { }
}
