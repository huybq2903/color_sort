/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-08-08


using System;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Level.Core
{
    [FAMessage("sc_force_play_level")]
    public class SCLevelForcePlay : SCMessage
    {
        public String levelData;
        public override void OnData()
        {
            FLevelManager.Instance.ForcePlayLevel(levelData);
        }
    }
}