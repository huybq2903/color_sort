/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-10
     */

namespace Falcon.Modules.Core.GameData.Runtime
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using Falcon.Helpers.EventBus;
	using Falcon.Modules.Core.AccountData;
	using Falcon.Modules.Core.BigData;
	using UnityEngine;

	/// <summary>
	/// Collector of <see cref="GameDataCore"/>.
	/// Using for manage resources. Useful for other modules want to manage resources
	/// but doesn't know any instance of resources but its id (string).
	/// </summary>
	public class ResourceCollector
	{
		private Action<string, int, string> _onResourceChange;
		
		private Dictionary<string, AResource> _resourcesMap = new();
		private Dictionary<string, FGameData> _gameDataMap  = new();
        
        public static System.Action onUpdateFromServerCallback;
		
        
		private static ResourceCollector _instance;

		public static ResourceCollector Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new ResourceCollector();
					_instance.Init();
				}
				
				return _instance;
			}
		}

		private void Init()
		{
			var data = GameDataCore.Instance;
			data.OnInit();
			
			foreach (var kvp in data.resources)
			{
				_resourcesMap.TryAdd(kvp.Key, kvp.Value);
				_gameDataMap.TryAdd(kvp.Key, data);
			}
            
            AccountManager.Instance.OnUpdateFromServer -= OnUpdateFromServer;
            AccountManager.Instance.OnUpdateFromServer += OnUpdateFromServer;
		}

        private void OnUpdateFromServer(ClientData clientData)
        {
	        // pre-check if server's data is null or empty. If so, do nothing...
	        var gameDatas = clientData.gameDatas;
	        if (gameDatas == null || gameDatas.Count == 0) return;
	        
	        var datas = gameDatas.Values;
	        if (datas.Count == 0) return;
	        
            _gameDataMap.Clear();
            _resourcesMap.Clear();

            bool existGameDataCore = false;
            
            var resourceType = typeof(AResource);
            foreach (var v in datas)
            {
                if (v is GameDataCore gameData)
                {
                    gameData.OnInit();
                    
                    foreach (var kvp in gameData.resources)
                    {
                        AddToResourcesMapInternal(kvp.Key, kvp.Value, v);
                    }
                    
                    existGameDataCore = true;
                    continue;
                }
                
                var type = v.GetType();
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (resourceType.IsAssignableFrom(field.FieldType))
                    {
                        var resourceInfo = field.FieldType.GetCustomAttribute<ResourceInfoAttribute>();
                        var resource     = (AResource)field.GetValue(v);
                        AddToResourcesMapInternal(resourceInfo.Id, resource, v);
                    }
                }
                
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (prop.CanWrite && prop.GetIndexParameters().Length == 0) // avoid indexers
                    {
                        if (resourceType.IsAssignableFrom(prop.PropertyType))
                        {
                            var resourceInfo = prop.PropertyType.GetCustomAttribute<ResourceInfoAttribute>();
                            var resource     = (AResource)prop.GetValue(v);
                            AddToResourcesMapInternal(resourceInfo.Id, resource, v);
                        }
                    }
                }
            }

            // in case no gameDataCore from server, use default.
            if (existGameDataCore == false)
            {
	            var data = GameDataCore.Instance;
	            data.OnInit();
	            
	            foreach (var kvp in data.resources)
	            {
		            AddToResourcesMapInternal(kvp.Key, kvp.Value, data);
	            }
            }
        
            onUpdateFromServerCallback?.Invoke();
        }

        private void AddToResourcesMapInternal(string resourceId, AResource resource, FGameData ownerInstanceData)
        {
            _resourcesMap.TryAdd(resourceId, resource);
            if (ownerInstanceData != null)
            {
                _gameDataMap.TryAdd(resourceId, ownerInstanceData);
            }
        }

		/// <summary>
		/// When an event <see cref="GameDataConst.EVENT_RESOURCE_GET"/> is emitted by other,
		/// it'll call <see cref="GetResourceValueInCollector"/> to return its value.
		/// </summary>
		internal void ListenEvents()
		{
			GameRequest<string, object>.Register(GameDataConst.EVENT_RESOURCE_GET, GetResourceValueInCollector);
			
			// one resource
			GameEvent<(string, int, string, string, string, Dictionary<string, object>)>.Register(GameDataConst.EVENT_RESOURCE_ADD, ResourceAddEventCall, null);
			GameEvent<(string, int, string, string, string, Dictionary<string, object>)>.Register(GameDataConst.EVENT_RESOURCE_REMOVE, ResourceRemoveEventCall, null);
		}

		/// <summary>
		/// Add custom <see cref="IResource"/> other than R to collector to tracking
		/// </summary>
		/// <param name="resource">Instance of R</param>
		/// <typeparam name="R">Type of <see cref="IResource"/></typeparam>
		public void AddToResourcesMap<R>(R resource) where R : AResource
		{
			var type = typeof(R);
			var resourceInfo = type.GetCustomAttribute<ResourceInfoAttribute>();
			if (resourceInfo != null)
			{
				_resourcesMap.TryAdd(resourceInfo.Id, resource);
			}
		}
		
		/// <summary>
		/// Add custom <see cref="IResource"/> other than R to collector to tracking,
		/// along with its owner instance for save/update server.
		/// </summary>
		/// <param name="resource">Instance of R</param>
		/// <param name="ownerInstanceData">Owner instance of R</param>
		/// <typeparam name="R">Type of <see cref="IResource"/></typeparam>
		public void AddToResourcesMap<R>(R resource, FGameData ownerInstanceData) where R : AResource
		{
			var type         = typeof(R);
			var resourceInfo = type.GetCustomAttribute<ResourceInfoAttribute>();
			if (resourceInfo != null)
			{
				_resourcesMap.TryAdd(resourceInfo.Id, resource);
                if (ownerInstanceData != null)
                {
                    _gameDataMap.TryAdd(resourceInfo.Id, ownerInstanceData);
                }
            }
		}

		public void AddResourceChangeListener(Action<string, int, string> callback)
		{
			_onResourceChange += callback;
		}
		
		public void RemoveResourceChangeListener(Action<string, int, string> callback)
		{
			_onResourceChange -= callback;
		}

		public void ResourceAdd(string resourceId, int amount, string data, 
			string @where = "", string itemId = "", 
			Dictionary<string, object> detail = null, FParam param = null)
		{
			if (_resourcesMap.TryGetValue(resourceId, out var resource))
			{
				if (resource.Add(amount, data))
				{
					long value = GetResourceValue(resource);
					long valueBefore = value - amount;
					ResourceLog.LogAdd(resourceId, amount, valueBefore, value, where, itemId, detail, param);

					_onResourceChange?.Invoke(resourceId, amount, data);
				}
			}
		}

		private void ResourceAddEventCall((string, int, string, string, string, Dictionary<string, object>) data)
		{
			ResourceAdd(data.Item1, data.Item2, data.Item3, data.Item4, data.Item5, data.Item6);
		}

		private long GetResourceValue(AResource resource)
		{
			long value = resource.GetInt;
			if (value <= 0)
			{
				var valueObject = resource.Get;
				if (valueObject is int i)
				{
					value = i;
				}
				else if (valueObject is long l)
				{
					value = l;
				}
			}

			return value;
		}
		
		public void ResourcesAdd((string resourceId, int amount, string data)[] resources, 
			string @where = "",
			Dictionary<string, object> detail = null, FParam param = null)
		{
			for (int i = 0; i < resources.Length; i++)
			{
				ResourceAdd(resources[i].resourceId, resources[i].amount, resources[i].data, where, where, detail, param);
			}
		}

		/// <summary>
		/// Get resource by id, return anew if not found
		/// </summary>
		public R GetResourceInCollector<R>(string resourceId) where R : AResource, new()
		{
			if(_resourcesMap.TryGetValue(resourceId, out var resource))
			{
				if (resource is R casted) return casted;
			}
			
			Debug.LogError($"Resource `{resourceId}` Not Found! Create a new `{typeof(R)}` and Add to Collector for using, but it'll not able to save");
			var newR = new R();
			_resourcesMap.TryAdd(resourceId, newR);
			
			return newR;
		}
		
		/// <summary>
		/// Get resource by id, return <see cref="DefaultResource"/> if not found
		/// </summary>
		public AResource GetResourceInCollector(string resourceId)
		{
			if(_resourcesMap.TryGetValue(resourceId, out var resource))
			{
				return resource;
			}

			Debug.LogError($"Resource `{resourceId}` Not Found! Return Default Resource");
			return new DefaultResource();
		}

		/// <summary>
		/// Get resource's value (GET)
		/// </summary>
		public object GetResourceValueInCollector(string resourceId)
		{
			return GetResourceInCollector(resourceId).Get;
		}

		/// <summary>
		/// Get resource's value (INT) - (GET)
		/// </summary>
		public int GetResourceValueIntInCollector(string resourceId)
		{
			return GetResourceInCollector(resourceId).GetInt;
		}
		
		public void ResourceRemove(string resourceId, int amount, string data, 
			string @where = "", string itemId = "", 
			Dictionary<string, object> detail = null, FParam param = null)
		{
			if (_resourcesMap.TryGetValue(resourceId, out var resource))
			{
				if (resource.Remove(amount, data))
				{
					long value = GetResourceValue(resource);
					long valueBefore = value + amount;
					ResourceLog.LogRemove(resourceId, amount, valueBefore, value, where, itemId, detail, param);
					
					_onResourceChange?.Invoke(resourceId, -amount, data);
				}
			}
		}
		
		private void ResourceRemoveEventCall((string, int, string, string, string, Dictionary<string, object>) data)
		{
			ResourceRemove(data.Item1, data.Item2, data.Item3, data.Item4, data.Item5, data.Item6);
		}

		public void ResourceSet(string resourceId, int value, string data, 
			string @where = "", string itemId = "", 
			Dictionary<string, object> detail = null, FParam param = null)
		{
			if (_resourcesMap.TryGetValue(resourceId, out var resource))
			{
				long valueBefore = GetResourceValue(resource);
				int valueChanged = resource.Set(value, data);

				if (valueChanged != 0)
				{
					ResourceLog.LogSet(resourceId, valueChanged, valueBefore, value, where, itemId, detail, param);

					_onResourceChange?.Invoke(resourceId, valueChanged, data);
				}
			}
		}

		public void ResourceReset(string resourceId, string @where = "", string  itemId = "", 
			Dictionary<string, object> detail = null, FParam param = null)
		{
			if (_resourcesMap.TryGetValue(resourceId, out var resource))
			{
				long valueBefore = GetResourceValue(resource);
				int  valuedReset = resource.Reset();
				if (valuedReset > 0)
				{
					long value = GetResourceValue(resource);
					ResourceLog.LogRemove(resourceId, valuedReset, valueBefore, value, where, itemId, detail, param);
					
					_onResourceChange?.Invoke(resourceId, -valuedReset, null);
				}
			}
		}

		/// <summary>
		/// Save and update server <see cref="FGameData"/> by resourceId
		/// </summary>
		/// <param name="resourceId">Resource Id</param>
		public void SaveAndUpdateServerOfResourceData(string resourceId)
		{
			if (_gameDataMap.TryGetValue(resourceId, out var data))
			{
				data?.UpdateToServer();
			}
			else
			{
				Debug.LogError($"Resource `{resourceId}` Not Found! Considering using AddToResourcesMap<>(string, FGameData) to add tracking");
			}
		}

		/// <summary>
		/// Save/update server all mapped <see cref="FGameData"/>
		/// </summary>
		public void SaveResourcesAndUpdateToServer()
		{
			var arrOfData = _gameDataMap.Values.Distinct();
			foreach (var data in arrOfData)
			{
				data?.UpdateToServer();
			}
		}
		
		public ResourceConfig GetResourceConfigById(string resourceId)
		{
			return ResourceConfigDatabase.Instance.GetConfig(resourceId);
		}
	}
}