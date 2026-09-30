using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.RewardFlow
{
    public class UIResourceSpawner : MonoBehaviour
    {
        public TMP_Text textAmount;
        public Image icon;
        public string[] resourceNames;
        public string where;

        private void Awake()
        {
            foreach (var s in resourceNames)
            {
                var rs = gameObject.AddComponent<UIResource>();
                rs.resourceName = s;
                rs.where = where;
                if (textAmount) rs.textAmount = textAmount;
                if (icon) rs.icon = icon;

                UIResourceManager.AddResource(rs);
            }
        }
    }
}