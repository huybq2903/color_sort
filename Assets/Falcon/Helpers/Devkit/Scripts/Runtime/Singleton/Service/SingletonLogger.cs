/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class SingletonLogger : MyLogger<SingletonLogger>
    {
        protected override string GetColor()
        {
            return "#7cadf8";
        }
    }
}