/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
*/

using UnityEngine;

namespace Falcon.Helpers.UI
{
    public static class ActiveChildByParentObject
    {
        public static void Update(GameObject parent, int indexChild)
        {
            var childCount = parent.transform.childCount;
            for (int i = 0; i < childCount; i++) parent.transform.GetChild(i).gameObject.SetActive(false);

            if (indexChild < 0 || indexChild >= childCount) return;

            for (int i = 0; i < childCount; i++)
            {
                var child = parent.transform.GetChild(i).gameObject;
                if (i == indexChild)
                {
                    child.SetActive(true);
                    break;
                }
            }
        }
        
        public static void Update(GameObject parent, string nameChild)
        {
            var childCount = parent.transform.childCount;
            for (int i = 0; i < childCount; i++) parent.transform.GetChild(i).gameObject.SetActive(false);
            
            if (string.IsNullOrEmpty(nameChild)) return;
    
            for (int i = 0; i < childCount; i++)
            {
                var child = parent.transform.GetChild(i).gameObject;
                if (child.name == nameChild)
                {
                    child.SetActive(true);
                    break;
                }
            }
        }
    }
}
