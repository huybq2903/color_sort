/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-15
 */

namespace Falcon.Manager.Importer.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public struct DefineMergeResult
    {
        public string       Result;
        public List<string> Added;
    }

    public static class GameTemplateImportLogic
    {
        public const string PackagePrefix = "game-template";

        public static bool IsGameTemplatePackage(string packageName)
        {
            if (string.IsNullOrEmpty(packageName)) return false;
            var name = Path.GetFileNameWithoutExtension(packageName);
            return name.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static DefineMergeResult MergeDefines(string current, IReadOnlyList<string> required)
        {
            var list = (current ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            var existing = new HashSet<string>(list, StringComparer.Ordinal);
            var added    = new List<string>();

            foreach (var sym in required)
            {
                if (existing.Contains(sym)) continue;
                list.Add(sym);
                existing.Add(sym);
                added.Add(sym);
            }

            return new DefineMergeResult { Result = string.Join(";", list), Added = added };
        }

        public static List<string> ScenesToAdd(IReadOnlyList<string> existingPaths, IReadOnlyList<string> candidatePaths)
        {
            var existing = new HashSet<string>(
                existingPaths ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var toAdd = new List<string>();

            foreach (var p in candidatePaths)
            {
                if (existing.Contains(p)) continue;
                if (toAdd.Contains(p))    continue;
                toAdd.Add(p);
            }

            return toAdd;
        }
    }
}
