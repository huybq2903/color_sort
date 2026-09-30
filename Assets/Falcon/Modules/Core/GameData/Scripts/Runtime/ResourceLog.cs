/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-09-16
     */


namespace Falcon.Modules.Core.GameData.Runtime
{
	using System.Collections.Generic;
	using Falcon.Helpers.EventBus;
	using Falcon.Modules.Core.BigData;
	using UnityEngine;

	public static class ResourceLog
	{
		public static void LogAdd(string resourceId, int amount, long valueBefore, long valueAfter, 
			string @where, string itemId, 
			Dictionary<string, object> detail = null, FParam param = null)
		{
			int currentLevel = GameRequest<int>.Request(GameDataConst.EVENT_GET_LEVEL);
			if (string.IsNullOrEmpty(where))
			{
				where = GetCurrentScene();	
			}

			if (string.IsNullOrEmpty(itemId))
			{
				itemId = where;
			}

			new ExtendResourceLog(FlowType.Source,
				where, currency: resourceId, itemId: ResolveItemId(itemId),
				amount, valueBefore, valueAfter, currentLevel, detail, param: ResolveParam(where, param)).Send();
		}

		public static void LogRemove(string resourceId, int amount, long valueBefore, long valueAfter, 
			string @where, string itemId, 
			Dictionary<string, object> detail = null, FParam param = null)
		{
			int currentLevel = GameRequest<int>.Request(GameDataConst.EVENT_GET_LEVEL);
			if (string.IsNullOrEmpty(where))
			{
				where = GetCurrentScene();
			}
			
			if (string.IsNullOrEmpty(itemId))
			{
				itemId = where;
			}
			
			new ExtendResourceLog(FlowType.Sink,
				where, currency: resourceId, itemId: ResolveItemId(itemId),
				amount, valueBefore, valueAfter, currentLevel, detail, param: ResolveParam(where, param)).Send();
		}

		public static void LogSet(string resourceId, int valueChanged, long valueBefore, long valueAfter, 
			string @where, string itemId, 
			Dictionary<string, object> detail = null, FParam param = null)
		{
			int absAmount = Mathf.Abs(valueChanged);
			if (valueChanged > 0)
			{
				LogAdd(resourceId, absAmount, valueBefore, valueAfter, where, itemId, detail, param);
			}
			else
			{
				LogRemove(resourceId, absAmount, valueBefore, valueAfter, where, itemId, detail, param);
			}
		}

		private static string ResolveItemId(string itemId)
		{
			return string.IsNullOrEmpty(itemId) ? FParam.UNKNOWN : itemId;
		}

		private static FParam ResolveParam(string @where, FParam param)
		{
			return param ?? new ResourceParam { resourceWhere = where };
		}

		private static string GetCurrentScene()
		{
			var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Trim();
			return sceneName.Replace(" ", "_").ToLower();
		}
	}
}