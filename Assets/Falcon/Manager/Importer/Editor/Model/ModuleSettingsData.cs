/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-04-24
 */


using System;
using System.Text;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    /// <summary>
    /// Represents a module folder under Assets/FalconAssets/Modules/
    /// </summary>
    public class ModuleInfo
    {
        public string Name;           // e.g., "UIModular"
        public string AssetPath;      // e.g., "Assets/FalconAssets/Modules/UIModular"

        public string DisplayName => CapitalizeSpaces(Name);

        private static string CapitalizeSpaces(string name)
        {
            // e.g., "UIModular" -> "UI Modular", "UIProfile" -> "UI Profile"
            var result = new StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && char.IsLower(name[i - 1]))
                    result.Append(' ');
                result.Append(name[i]);
            }
            return result.ToString();
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// Represents a ScriptableObject .asset file inside a module folder
    /// </summary>
    public class SOAssetInfo
    {
        public string Name;           // e.g., "StaminaSO"
        public string AssetPath;      // e.g., "Assets/FalconAssets/Modules/UIModular/UILives/ScriptableObject/Resources/StaminaSO.asset"
        public UnityEngine.Object Instance; // loaded SO instance
        public Type Type => Instance != null ? Instance.GetType() : null;
    }
}