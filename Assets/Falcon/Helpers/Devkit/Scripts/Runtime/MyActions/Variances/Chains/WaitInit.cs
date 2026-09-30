/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class WaitInit : ChainAction
    {
        public WaitInit(IContinuableAction action) : base(action)
        {
        }

        public WaitInit(Action action) : this(new UnitAction(action))
        {
        }

        public override bool CanInvoke()
        {
            return InitService.AllInitState.IsDone() && base.CanInvoke();
        }
    }
}