/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-24


using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PlasticGui.WorkspaceWindow.CodeReview.ReviewChanges.Summary;

namespace Falcon.Modules.Core.ModulesVerify.Editor
{
    public class FModule
    {
        public string name;
        public string path;
        public string author;
        public List<string> dependencies = new List<string>();
        public string version;
        public bool status;
        public Dictionary<string, (bool, string)> verified = new Dictionary<string, (bool, string)>();
        
        public bool HasTextInModule(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;

            string targetPath = Path.GetDirectoryName(this.path); 
            foreach (string file in Directory.EnumerateFiles(targetPath, "*.cs", SearchOption.AllDirectories))
            {
                foreach (string line in File.ReadLines(file))
                {
                    if (line.Contains(text))
                        return true;
                }
            }
            return false;
        }

        public bool HasTextInAllFiles(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;

            string targetPath = Path.GetDirectoryName(this.path); 
            bool hasTextInAllFiles = true;
            foreach (string file in Directory.EnumerateFiles(targetPath, "*.cs", SearchOption.AllDirectories))
            {
                string relativeDir = Path.GetDirectoryName(file);
                string[] pathParts = relativeDir.Split(Path.DirectorySeparatorChar);

                // Kiểm tra nếu bất kỳ thư mục cha nào bắt đầu bằng Lib, Test, Editor
                if (pathParts.Any(part => part.StartsWith("Lib") || part.StartsWith("Test") || part.StartsWith("Editor")))
                {
                    continue; // Bỏ qua file này
                }
                
                bool fileHasText = false;
                foreach (string line in File.ReadLines(file))
                {
                    if (line.Contains(text))
                    {
                        fileHasText = true;
                        break;  
                    }
                }
                
                hasTextInAllFiles &= fileHasText;
            }
            return hasTextInAllFiles;
        }
    }
}