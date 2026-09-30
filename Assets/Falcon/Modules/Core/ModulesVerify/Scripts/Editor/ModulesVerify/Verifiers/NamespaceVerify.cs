/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-27


using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Falcon.Modules.Core.ModulesVerify.Editor
{
    [FAModuleVerify("Namespace theo chuẩn Falcon")]
    
    public class NamespaceVerify : IModuleVerify
    {
        public (bool, string) VerifyModules(FModule module)
        {
            string targetPath = Path.GetDirectoryName(module.path); 
            string[] csFiles = Directory.GetFiles(targetPath, "*.cs", SearchOption.AllDirectories);

            Dictionary<string, List<string>> namespaceToFiles = new Dictionary<string, List<string>>();
            Regex namespaceRegex = new Regex(@"\bnamespace\s+([a-zA-Z0-9_.]+)");

            foreach (string file in csFiles)
            {
                string content = File.ReadAllText(file);
                Match match = namespaceRegex.Match(content);
                string ns = match.Success ? match.Groups[1].Value : "(No Namespace)";

                if (!namespaceToFiles.ContainsKey(ns))
                    namespaceToFiles[ns] = new List<string>();

                namespaceToFiles[ns].Add(file);
            }


            if (namespaceToFiles.Count == 0)
            {
                return (true, "");
            }
            else if (namespaceToFiles.Count == 1)
            {
                if (!namespaceToFiles.Keys.First().Contains("Falcon.")) 
                    return (false, "Namespace phải bắt đầu bằng Falcon.");
                else
                    return (true, "");
            }
            else
            {
                return (false, $"Nhiều hơn 1 Namespace trong Module!");
            }
        }
    }
}