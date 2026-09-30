/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-30
     */


namespace Falcon.Manager.Shared
{
	using System;
	using UnityEditor;
	using UnityEngine;

	public static class AuthKeyRepository
	{
		private const string TOKEN_KEY = "TOKEN_KEY";
		
		public static void Save(GoogleAuthKey authKey)
		{
			EditorPrefs.SetString(TOKEN_KEY, JsonUtility.ToJson(authKey));
		}
        
		public static GoogleAuthKey GetKey()
		{
			return TryGetKey(out var key) ? key : throw new Exception("GGAuthKey not found");
		}

		public static bool HasKey()
		{
			return EditorPrefs.HasKey(TOKEN_KEY);
		}
        
		public static void DeleteKey()
		{
			EditorPrefs.DeleteKey(TOKEN_KEY);
		}
		
		public static bool TryGetKey(out GoogleAuthKey authKey)
		{
			if (EditorPrefs.HasKey(TOKEN_KEY))
			{
				try
				{
					authKey = JsonUtility.FromJson<GoogleAuthKey>(EditorPrefs.GetString(TOKEN_KEY));
					return true;
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
			
			authKey = default;
			return false;
		}
	}
}