/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-12
     */


namespace Falcon.Modules.Core.GameData.Runtime
{
	using UnityEngine;

	public static class GameDataRunner
	{
		// after awake
		public static void SceneLoad()
		{
			Init();
		}

		private static void Init()
		{
			ResourceCollector.Instance.ListenEvents();
		}
	}
}