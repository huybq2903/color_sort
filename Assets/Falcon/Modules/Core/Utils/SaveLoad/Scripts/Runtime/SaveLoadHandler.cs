/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-03
     */

using System;
using System.Security.Cryptography;
using UnityEngine;

namespace Falcon.Modules.Core.SaveLoad.Runtime
{
	using BayatGames.SaveGamePro;
	using Falcon.Helpers.Security;

	public static class SaveLoadHandler
    {
	    private static ISaveLoadConfigs _saveLoadConfigs;

	    public static ISaveLoadConfigs Configs
	    {
		    get
		    {
			    if (_saveLoadConfigs == null)
			    {
				    _saveLoadConfigs = new DefaultSaveLoadConfigs();
			    }
			    
			    return _saveLoadConfigs;
		    }
		    
		    set => _saveLoadConfigs = value;
	    }
	    
	    
	    private static SaveGameSettings _saveGameSettings = new();
	    private static SaveGameSettings GetSaveGameSettings 
	    {
		    get
		    {
			    _saveGameSettings.Encrypt            = true;
			    _saveGameSettings.EncryptionPassword = Configs.SaveLoadDefaultPass;
			    return _saveGameSettings;
		    }
	    }

		private static readonly object kLock = new object();

		/// <summary>
		/// Encrypted key save
		/// </summary>
		public static void Save<T>(string key, T obj)
		{
			lock (kLock)
			{
				string mKey = GetEncryptedKey(key);

				try
				{
					SaveGame.Save<T>(mKey, obj, GetSaveGameSettings);
				}
				catch (CryptographicException)
				{
					try
					{
						SaveGame.Delete(mKey, GetSaveGameSettings);
						SaveGame.Save<T>(mKey, obj, GetSaveGameSettings);
					}
					catch (Exception)
					{
						/* Ignored */
					}
				}
				catch (Exception)
				{
					try
					{
						SaveGame.Delete(mKey, GetSaveGameSettings);
						SaveGame.Save<T>(mKey, obj, GetSaveGameSettings);
					}
					catch (Exception)
					{
						/* Ignored */
					}
				}
			}
		}

		private static string GetEncryptedKey(string key)
	    {
		    return Encryption.MD5(key);
	    }

	    /// <summary>
	    /// Load with default value
	    /// </summary>
	    public static T Load<T>(string key, T defaultValue = default)
	    {
			lock (kLock)
		    { 
				T obj = defaultValue;
				var mEncryptedKey = GetEncryptedKey(key);

				if (!ExistsKeyInternal(mEncryptedKey))
					return obj;

				try
				{
					obj = SaveGame.Load<T>(mEncryptedKey, defaultValue, GetSaveGameSettings);
				}
				catch (CryptographicException)
				{
					try { SaveGame.Delete(mEncryptedKey, GetSaveGameSettings); }
					catch (Exception) { /* Ignored */ }
					obj = defaultValue;
				}
				catch (Exception)
				{
					try { SaveGame.Delete(mEncryptedKey, GetSaveGameSettings); }
					catch (Exception) { /* Ignored */ }
					obj = defaultValue;
				}

				return obj;
			}
	    }

	    
	    public static object Load(string key, object defaultValue = default)
	    {
			lock (kLock)
		    {
				object obj = defaultValue;
				var mEncryptedKey = GetEncryptedKey(key);

				if (!ExistsKeyInternal(mEncryptedKey))
					return obj;

				try
				{
					obj = SaveGame.Load(mEncryptedKey, defaultValue, GetSaveGameSettings);
				}
				catch (CryptographicException)
				{
					try { SaveGame.Delete(mEncryptedKey, GetSaveGameSettings); }
					catch (Exception) { /* Ignored */ }
					obj = defaultValue;
				}
				catch (Exception)
				{
					try { SaveGame.Delete(mEncryptedKey, GetSaveGameSettings); }
					catch (Exception) { /* Ignored */ }
					obj = defaultValue;
				}

				return obj;
			}
	    }


	    /// <summary>
	    /// Generate key by object
	    /// </summary>
	    public static string GenerateKeyBy<T>(T obj)
	    {
		    var combinedKey = string.Format($"{Configs.SaveLoadRandomKey}{obj}");
		    return combinedKey;
	    }

	    /// <summary>
	    /// Check key exists
	    /// </summary>
	    public static bool ExistsKey(string key)
	    {
		    var mEncryptedKey = GetEncryptedKey(key);
		    return ExistsKeyInternal(mEncryptedKey);
	    }
	    
	    private static bool ExistsKeyInternal(string key)
	    {
		    return SaveGame.Exists(key, GetSaveGameSettings);
	    }

	    /// <summary>
	    /// Delete key
	    /// </summary>
	    public static void DeleteKey(string key)
	    {
		    var mEncryptedKey = GetEncryptedKey(key);
		    SaveGame.Delete(mEncryptedKey, GetSaveGameSettings);
	    }
    }
}