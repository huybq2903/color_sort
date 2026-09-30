/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Falcon.Modules.Core.GameData.Runtime
{
	using Falcon.Helpers.FReflection;

	public static class GameDataCoreRegistry
	{
		private static readonly Dictionary<string, Type> _map = new();
        
		private static  void Register(string typeName, Type type) => _map[typeName] = type;

		public static Type Get(string typeName) => _map.TryGetValue(typeName, out var type) ? type : null;

        public static void Init()
        {
            if (_map.Count > 0) return;
            
            var resourceTypes = FReflection.Instance.GetTypes().Where(t => typeof(AResource)
                .IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Where(t => t.GetCustomAttribute<ResourceInfoAttribute>() != null);
            
			foreach (var type in resourceTypes)
			{
				var attr = type.GetCustomAttribute<ResourceInfoAttribute>();
				Register(attr.Id, type);
			}
		}
	}
}