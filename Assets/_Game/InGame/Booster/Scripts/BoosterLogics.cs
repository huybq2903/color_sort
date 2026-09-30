/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-29
 */

using Falcon.Shared.BaseBooster;
using UnityEngine;
using UnityEngine.Scripting;

namespace Falcon.InGame.Booster
{
    [Preserve]
    public class Booster1Logic : ABoosterLogic
    {
        public override string Type => BoosterConst.BOOSTER_1;
        public override void Execute() => Debug.Log($"[Booster] {Type} executed, left {_resource.Quantity}, free {_resource.IsFree}");
    }

    [Preserve]
    public class Booster2Logic : ABoosterLogic
    {
        public override string Type => BoosterConst.BOOSTER_2;
        public override void Execute() => Debug.Log($"[Booster] {Type} executed, left {_resource.Quantity}, free {_resource.IsFree}");
    }

    [Preserve]
    public class Booster3Logic : ABoosterLogic
    {
        public override string Type => BoosterConst.BOOSTER_3;
        public override void Execute() => Debug.Log($"[Booster] {Type} executed, left {_resource.Quantity}, free {_resource.IsFree}");
    }
}
