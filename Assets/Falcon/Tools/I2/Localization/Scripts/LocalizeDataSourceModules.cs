using System.Collections.Generic;
using UnityEngine;

namespace I2.Loc
{
#if UNITY_EDITOR
    [CreateAssetMenu(fileName = "I2DataNameSourceModules", menuName = "I2 Localization/DataNameSourceModules", order = 1)]
#endif
    public class LocalizeDataSourceModules : ScriptableObject
    {
        public List<LanguageSourceAsset> modules = new List<LanguageSourceAsset>();
    }
}
