#if UNITY_IOS && UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public static class FsnIOSResourcePostProcessor
    {
        private static readonly string SourceRelativeDir =
            "Falcon/Modules/Core/ThirdParty/Mediation/Scripts/Runtime/Service/FSN/Plugins/iOS";

        private static readonly string[] ImageNames =
        {
            "fsn_ad_close_ic.png",
            "fsn_collapsible_countdown_ic.png",
            "fsn_ad_close_ic_fake.png",
            "fsn_collapsible_close_ic1.png"
        };

        [PostProcessBuild(1000)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            string sourceDir = Path.Combine(Application.dataPath, SourceRelativeDir);
            string destDir = Path.Combine(pathToBuiltProject, "FsnAdResources");

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            string pbxPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);

            PBXProject pbxProject = new PBXProject();
            pbxProject.ReadFromFile(pbxPath);

#if UNITY_2019_3_OR_NEWER
            string mainTargetGuid = pbxProject.GetUnityMainTargetGuid();
#else
            string mainTargetGuid = pbxProject.TargetGuidByName("Unity-iPhone");
#endif

            foreach (string imageName in ImageNames)
            {
                string sourcePath = Path.Combine(sourceDir, imageName);
                string destPath = Path.Combine(destDir, imageName);

                if (!File.Exists(sourcePath))
                {
                    Debug.LogWarning("[FsnIOSResourcePostProcessor] Missing source image: " + sourcePath);
                    continue;
                }

                File.Copy(sourcePath, destPath, true);

                string projectRelativePath = "FsnAdResources/" + imageName;

                string fileGuid = pbxProject.FindFileGuidByProjectPath(projectRelativePath);
                if (string.IsNullOrEmpty(fileGuid))
                {
                    fileGuid = pbxProject.AddFile(
                        projectRelativePath,
                        projectRelativePath,
                        PBXSourceTree.Source
                    );
                }

                pbxProject.AddFileToBuild(mainTargetGuid, fileGuid);

                Debug.Log("[FsnIOSResourcePostProcessor] Added iOS resource: " + projectRelativePath);
            }

            pbxProject.WriteToFile(pbxPath);
        }
    }
}

#endif