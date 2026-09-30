/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-27


using System.IO;
using Newtonsoft.Json;

namespace Falcon.Modules.Core.ModulesVerify.Editor
{
    [FAModuleVerify("Author")]
    public class AuthorVerify : IModuleVerify
    {
        public (bool, string) VerifyModules(FModule module)
        {
            string modulePath = Path.GetDirectoryName(module.path);
            while(File.Exists(modulePath + "/package.json") == false && 
                  !string.IsNullOrEmpty(modulePath) && 
                  modulePath != "Assets")
            {
                modulePath = Path.GetDirectoryName(modulePath);
            }
        
            string packagePath = modulePath + "/package.json";
            if (File.Exists(packagePath))
            {
                string jsonConfig = File.ReadAllText(packagePath);
                PackageJson package = JsonConvert.DeserializeObject<PackageJson>(jsonConfig);
                if (package != null && package.author != null)
                {
                    if (package.author.name.CompareTo("author") != 0)
                        return (true, package.author.name); 
                    else
                    {
                        return (false, package.author.name);
                    }
                }

            } 
            return (false, "");
        }
    }
}