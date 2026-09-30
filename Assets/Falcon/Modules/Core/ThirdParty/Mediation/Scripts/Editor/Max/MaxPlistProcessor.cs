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
using System;
using UnityEngine;
using UnityEditor.iOS.Xcode;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Editor
{
    /// <summary>
    /// PostProcessor script to automatically fill all required dependencies
    /// </summary>
    public class ISPlistProcessor
    {
#if AMAZON_ENABLE
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget == BuildTarget.iOS)
            {
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

                    // Check if SKAdNetworkItems already exists
                    PlistElementArray SKAdNetworkItems = null;
                    if (rootDict.values.ContainsKey("SKAdNetworkItems"))
                    {
                        try
                        {
                            SKAdNetworkItems = rootDict.values["SKAdNetworkItems"] as PlistElementArray;
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning(string.Format("Could not obtain SKAdNetworkItems PlistElementArray: {0}",
                                e.Message));
                        }
                    }

                    if (SKAdNetworkItems == null)
                    {
                        SKAdNetworkItems = rootDict.CreateArray("SKAdNetworkItems");
                    }

                    string plistContent = File.ReadAllText(buildPath);

                    string[] str = new string[]
                    {
                        //amazon
                        "p78axxw29g.skadnetwork"
                    };

                    for (int i = 0; i < str.Length; i++)
                    {
                        if (!plistContent.Contains(str[i]))
                        {
                            PlistElementDict SKAdNetworkIdentifierDict = SKAdNetworkItems.AddDict();
                            SKAdNetworkIdentifierDict.SetString("SKAdNetworkIdentifier", str[i]);
                        }
                    }

                    File.WriteAllText(plistPath, plist.WriteToString());
                }
            }
        }
#endif
    }
}

#endif