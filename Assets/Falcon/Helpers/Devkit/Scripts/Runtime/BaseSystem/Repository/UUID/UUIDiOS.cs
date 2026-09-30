/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-30
 */

using System.Runtime.InteropServices;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class UUIDiOS : MonoBehaviour {
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
