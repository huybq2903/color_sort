/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

#pragma warning disable 0618
#if UNITY_IOS || UNITY_IPHONE
using System.IO;
using UnityEditor.Callbacks;
using UnityEditor;
using UnityEditor.iOS.Xcode;

namespace Falcon.Modules.Core.ThirdParty.Mmp.Editor
{
    /// <summary>
    /// PostProcessor script to automatically fill all required dependencies
    /// </summary>
    public class ISPlistProcessor
    {
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget == BuildTarget.iOS)
            {
#if APPSFLYER_ENABLE
                string plistPath = Path.Combine(buildPath, "Info.plist");
                PBXProject project = new PBXProject();
                string projectPath = PBXProject.GetPBXProjectPath(buildPath);
                project.ReadFromFile(projectPath);
                PlistDocument plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));
                if (plist != null)
                {
                    // Get root
                    PlistElementDict rootDict = plist.root;

                    string plistContent = File.ReadAllText(plistPath);
                    if (!plistContent.Contains("NSAppTransportSecurity"))
                    {
                        PlistElementDict NSAppTransportSecurity = rootDict.CreateDict("NSAppTransportSecurity");
                        NSAppTransportSecurity.SetBoolean("NSAllowsArbitraryLoads", true);
                    }

                    File.WriteAllText(plistPath, plist.WriteToString());
                }
#endif
            }
        }
    }
}

#endif