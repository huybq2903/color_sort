/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-22

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Falcon.Modules.Core.Network;
using NUnit.Framework.Internal;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Falcon.Modules.Core.ModulesVerify.Editor
{
    [FAModuleVerify("CS, SC")]
    public class CSSCVerify : IModuleVerify
    {

        public (bool, string) VerifyModules(FModule module)
        {
            string targetPath = Path.GetDirectoryName(module.path);
            string[] csFiles = Directory.GetFiles(targetPath, "*.cs", SearchOption.AllDirectories);

            List<string> errors = new();
            Regex classRegex = new Regex(@"\bclass\s+([a-zA-Z0-9_]+)\s*:\s*([a-zA-Z0-9_]+)");
            Regex attributeRegex = new Regex(@"\[FAMessage\s*\(\s*\"".+?\""\s*\)\s*\]");

            foreach (string file in csFiles)
            {
                string content = File.ReadAllText(file);

                Match classMatch = classRegex.Match(content);
                if (!classMatch.Success)
                    continue;

                string className = classMatch.Groups[1].Value;
                string baseClass = classMatch.Groups[2].Value;

                if (baseClass != "FMessage")
                    continue;

                // Kiểm tra tên class
                if (!(className.StartsWith("CS") || className.StartsWith("SC")))
                {
                    errors.Add($"❌ {className} (in {Path.GetFileName(file)}) should start with CS or SC.");
                }

                // Kiểm tra attribute
                if (!attributeRegex.IsMatch(content) && !className.Equals("CSMessage") && !className.Equals("SCMessage"))
                {
                    errors.Add($"❌ {className} (in {Path.GetFileName(file)}) is missing [FAMessage(\"...\")].");
                }
            }

            if (errors.Count == 0)
                return (true, "");

            string errorMessage = string.Join("\n", errors);
            return (false, errorMessage); 
        }
    }
}