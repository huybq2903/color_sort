/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-09
 */

using Falcon.Helpers.FReflection;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    [FReflection]
    public interface ISingletonLogic
    {
        int Priority { get; }
    }
}