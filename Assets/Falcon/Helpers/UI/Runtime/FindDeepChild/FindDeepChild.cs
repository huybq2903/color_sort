/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-2
*/

using UnityEngine;

namespace Falcon.Helpers.UI
{
    public static class FindDeepChild
    {
        public static Transform Find(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                Transform result = Find(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
