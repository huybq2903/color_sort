/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-06
     */


namespace Falcon.Modules.Core.Utils.Time.Runtime
{
	using System;
	using Falcon.Modules.Core.Network;
	using UnityEngine;

	/// <summary>
	/// Utils class for using inside module
	/// </summary>
	internal static class AntiTimeCheatUtils
	{
		internal static Action<DateTime> onTimeServerUpdated;
		
		internal static DateTime GetDateTimeUTC()
		{
			return DateTime.UtcNow;
		}

		internal static void ListenToUpdateTimeFromServer()
		{
			FNetManager.Instance.OnSessionStarted(UpdateFollowTimeServer);
		}
		
		private static void UpdateFollowTimeServer()
		{
			new FTimePing().AddSCListener<FTimePong>((message, timeout, success) =>
			{
				if (success)
				{
					var            timeServer = message.timeServer;
					DateTimeOffset date       = DateTimeOffset.FromUnixTimeMilliseconds(timeServer);
					var            utcNow     = date.UtcDateTime;
					onTimeServerUpdated?.Invoke(utcNow);
				}
			}).Send();
		}
	}
}