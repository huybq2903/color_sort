/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-23
     */


using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Falcon.Manager.Shared
{
    public static class ChangelogManager
    {
        internal static Dictionary<string, string> ReadChangelog(string changelogPath)
        {
            var changelog = new Dictionary<string, string>();
            if (!File.Exists(changelogPath))
            {
                Debug.LogError("CHANGELOG.md not found at " + changelogPath);
                return changelog;
            }

            var    lines          = File.ReadAllLines(changelogPath);
            string currentVersion = null;
            var    currentLog     = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"^##\s*(\d+\.\d+\.\d+)");
                if (match.Success)
                {
                    if (currentVersion != null)
                    {
                        changelog[currentVersion] = currentLog.ToString().Trim();
                    }

                    currentVersion = match.Groups[1].Value;
                    currentLog.Clear();
                }
                else if (currentVersion != null)
                {
                    currentLog.AppendLine(line);
                }
            }

            if (currentVersion != null)
            {
                changelog[currentVersion] = currentLog.ToString().Trim();
            }

            return changelog;
        }

        public static bool IsExistVersion(string changelogPath, string version)
        {
            var changelog = ReadChangelog(changelogPath);
            return changelog != null && changelog.ContainsKey(version);
        }

        internal static void WriteChangelog(Dictionary<string, string> changelog, string changelogPath)
        {
            var sortedVersions = changelog.Keys.ToList()
                .OrderByDescending(v => new Version(v))
                .ToList();

            var content = new System.Text.StringBuilder();
            var separator = "----------------------------------";
            foreach (var version in sortedVersions)
            {
                content.AppendLine($"## {version}");
                
                var log = changelog[version];
                content.AppendLine(log);

                if (!log.Contains(separator))
                {
                    content.AppendLine(separator);
                }

                content.AppendLine();
                content.AppendLine();
            }

            File.WriteAllText(changelogPath, content.ToString().TrimEnd());
            AssetDatabase.Refresh();
        }

        public static string GetChangelogContent(string changelogPath)
        {
            if (!File.Exists(changelogPath))
            {
                return string.Empty;
            }
            
            return File.ReadAllText(changelogPath);
        }

        public static void AddNewVersion(string version, string log, string changelogPath)
        {
            if (!Regex.IsMatch(version, @"^\d+\.\d+\.\d+$"))
            {
                Debug.LogError("Invalid version format. Please use format like 1.0.0");
                return;
            }

            var changelog = ReadChangelog(changelogPath);
            changelog[version] = log;
            WriteChangelog(changelog, changelogPath);

            Debug.Log($"Added version {version} in CHANGELOG.md");
        }
    }
} 