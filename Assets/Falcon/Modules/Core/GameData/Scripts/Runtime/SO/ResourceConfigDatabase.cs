/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-12
     */


namespace Falcon.Modules.Core.GameData.Runtime
{
    using System.Collections.Generic;
    using UnityEngine;

    public class ResourceConfigDatabase : ScriptableObject
    {
	    public const string PATH = @"Assets/FalconAssets/Modules/Core/GameData/Resources/";
	    public const string NAME = @"SO_FCM_ResourceConfigDatabase";

	    public List<ResourceConfig> configs = new();

	    private static ResourceConfigDatabase _instance;
	    public static ResourceConfigDatabase Instance
	    {
		    get
		    {
			    if (_instance == null)
			    {
				    _instance = Resources.Load<ResourceConfigDatabase>(NAME);
			    }

			    return _instance;
		    }
	    }
	    
        public ResourceConfig GetConfig(string id)
        {
	        return configs.Find(c => c.id == id);
        }
    }
}
