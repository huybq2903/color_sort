/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-29
 */

using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Shared.BaseBooster;
using UnityEngine.Scripting;

namespace Falcon.InGame.Booster
{
    // Id phải khớp type trong SO_BoosterInfo. Preserve vì chỉ được tạo qua reflection.
    [ResourceInfo("booster_1"), Preserve]
    public class Booster1Resource : ABoosterResource { }

    [ResourceInfo("booster_2"), Preserve]
    public class Booster2Resource : ABoosterResource { }

    [ResourceInfo("booster_3"), Preserve]
    public class Booster3Resource : ABoosterResource { }
}
