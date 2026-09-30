/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-04
     */

using System.Runtime.InteropServices;

namespace Falcon.Modules.Core.SystemInformation.Runtime
{
	public static class UUIDiOS
	{
#if UNITY_IOS
        [DllImport ("__Internal")]
        private static extern void DeleteStr(string key);

        [DllImport ("__Internal")]
        private static extern void SaveStr(string key, string value);

        [DllImport ("__Internal")]
        private static extern string GetStr(string key);


        public static void SaveKeyChainValue(string key, string value)
        {
            SaveStr(key, value);
        }

        public static void DeleteKeyChainValue(string key)
        {
            DeleteStr(key);
        }

        public static string GetKeyChainValue(string key)
        {
            // if return null mean null.
            var result = GetStr(key);
            return result != "null" ? result : null;
        }
#endif
	}
}