/*
     * Author: minhddv
     * Email: minhddv@falcongames.com
     * Company: Falcon Games
     * Date: 2025-10-28
*/

using System;
using UnityEngine;

namespace Falcon.Modules.InAppUpdate.Scripts.Runtime
{
    public static class InAppUpdateHelper
    {
        private static int? CompareVersions(string versionA, string versionB)
        {
            if (string.IsNullOrEmpty(versionA) || string.IsNullOrEmpty(versionB))
            {
                Debug.LogError("InAppUpdateHelper > version string không được null hoặc rỗng.");
                return null;
            }

            string[] partsA = versionA.Split('.');
            string[] partsB = versionB.Split('.');

            int maxLen = Math.Max(partsA.Length, partsB.Length);
            for (int i = 0; i < maxLen; i++)
            {
                int a = 0, b = 0;

                if (i < partsA.Length && !int.TryParse(partsA[i], out a))
                {
                    Debug.LogError($"InAppUpdateHelper > versionA không hợp lệ");
                    return null;
                }

                if (i < partsB.Length && !int.TryParse(partsB[i], out b))
                {
                    Debug.LogError($"InAppUpdateHelper > versionB không hợp lệ");
                    return null;
                }

                if (a < b) return -1;
                if (a > b) return 1;
            }

            return 0;
        }

        /// <summary>
        /// Kiểm tra xem currentVersion có nhỏ hơn targetVersion không.
        /// </summary>
        public static bool? CompareLower(string currentVersion, string targetVersion)
        {
            var result = CompareVersions(currentVersion, targetVersion);
            if (result == null) return null;
            return result < 0;
        }
    }
}