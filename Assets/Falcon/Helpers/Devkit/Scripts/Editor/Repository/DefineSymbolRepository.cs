/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public static class DefineSymbolRepository
    {
        private const char kDefineSeparator = ';';

        public static void Add(params string[] defines)
        {
            List<string> currentSymbols = GetCurrentSymbols().ToList();
            currentSymbols.AddRange(defines.Except(currentSymbols));
            UpdateSymbols(currentSymbols);
        }
 
        public static void Remove(params string[] defines)
        {
            List<string> currentSymbols = GetCurrentSymbols().ToList();
            currentSymbols.RemoveAll(defines.
                Contains);
            UpdateSymbols(currentSymbols);
        }
 
        public static void Clear()
        {
            UpdateSymbols(new List<string>());
        }
 
        // Updated to use NamedBuildTarget.FromBuildTargetGroup
        private static IEnumerable<string> GetCurrentSymbols()
        {
            // Get the NamedBuildTarget for the currently selected build target GROUP.
            NamedBuildTarget currentNamedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            // Use the new PlayerSettings.GetScriptingDefineSymbols API
            return PlayerSettings.GetScriptingDefineSymbols(currentNamedBuildTarget)
                   .Split(kDefineSeparator)
                   .Where(s => !string.IsNullOrWhiteSpace(s))
                   .Select(s => s.Trim());
        }
 
        // Updated to use NamedBuildTarget.FromBuildTargetGroup
        private static void UpdateSymbols(List<string> updatedSymbols)
        {
            string symbolsString = string.Join(kDefineSeparator, updatedSymbols.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray());
            
            // Get the NamedBuildTarget for the currently selected build target GROUP.
            NamedBuildTarget currentNamedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            // Use the new PlayerSettings.SetScriptingDefineSymbols API
            PlayerSettings.SetScriptingDefineSymbols(currentNamedBuildTarget, symbolsString);

            // Important: Force Unity to recompile scripts to apply new symbols immediately
            AssetDatabase.Refresh();
        }
    }
}