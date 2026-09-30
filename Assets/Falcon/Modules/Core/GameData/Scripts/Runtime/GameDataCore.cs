/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-10
     */


namespace Falcon.Modules.Core.GameData.Runtime
{
	using System.Collections.Generic;
	using System.Reflection;
	using Falcon.Modules.Core.AccountData;
	using Newtonsoft.Json;
	using UnityEngine;

	[FGameDataType("game_data_core")]
	public class GameDataCore : FGameData<GameDataCore>
	{
		[JsonConverter(typeof(ResourcesConverter))]
		public Dictionary<string, AResource> resources = new();
		
		internal void OnInit()
		{
			AddResourceType(new GoldResource());
			ResourceInjector.Injection(AddResourceType);
		}

		/// <summary>
		/// Add resource for save/load
		/// </summary>
		private void AddResourceType(AResource resource)
		{
			var t            = resource.GetType();
			var resourceInfo = t.GetCustomAttribute<ResourceInfoAttribute>();
			if (resourceInfo != null)
			{
				resources.TryAdd(resourceInfo.Id, resource);
			}
		}
	}
}