/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
 */


namespace Falcon.Modules.Core.GameData.Runtime
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using System.Reflection;
	using UnityEngine;

	public class ResourcesConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var list = (Dictionary<string, AResource>)value;
			var jo   = new JObject();

			foreach (var kv in list)
			{
				var data  = kv.Value;
				var token = JObject.FromObject(data, serializer);

				jo[kv.Key] = token;
			}

			jo.WriteTo(writer);
		}
		
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var result = new Dictionary<string, AResource>();
			var jo     = JObject.Load(reader);
            
            GameDataCoreRegistry.Init();

			foreach (var prop in jo.Properties())
			{
				var value = prop.Value;

				var targetType = GameDataCoreRegistry.Get(prop.Name);
				if (targetType == null)
					continue;

				var gameData = (AResource)value.ToObject(targetType, serializer);
				result[prop.Name] = gameData;
			}

			return result;
		}
		
		public override bool CanConvert(Type objectType)
		{
			return typeof(IResource).IsAssignableFrom(objectType);
		}
	}
}