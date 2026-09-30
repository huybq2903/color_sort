/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-30


using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Falcon.Modules.Core.ModulesVerify.Editor
{
    [FAModuleVerify("Sử dụng FReflection")]
    public class ReflectionVerify : IModuleVerify
    {
        public (bool, string) VerifyModules(FModule module)
        {
            if (module.name.Contains("FReflection"))
                return (true, "");
            if (module.HasTextInModule("AppDomain.CurrentDomain"))
            {
                return (false, "Sử dụng FReflection thay cho AppDomain.CurrentDomain.GetAssemblies()");
            }
            else
            {
                return (true, "");
            }
        }
    }
}