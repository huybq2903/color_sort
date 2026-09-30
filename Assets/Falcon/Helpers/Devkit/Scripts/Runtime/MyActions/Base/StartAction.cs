/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */
// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public abstract class StartAction : MyAction, IStartAction
    {
        public override bool CanInvoke()
        {
            return true;
        }
    }
}