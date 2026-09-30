/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-12
     */

using UnityEngine;

namespace Falcon.Modules.Core.GameData.Runtime
{
	using Sirenix.OdinInspector;

	[System.Serializable]
    public class ResourceConfig
    {
	    [ReadOnly]
        public string id;
        
        public string name;
        
        [PreviewField] public Sprite iconSmall;
        
        [PreviewField(Height = 75)] public Sprite iconLarge;
    }
} 