/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Falcon.Modules.Core.Network
{
    public class LogUtil
    {
        public static void Log(FMessage message)
        {

  #if UNITY_EDITOR || BUILD_TEST
            if (message is FPing || message is FPong)
                return;
            JsonDotNetEncoder jsonDotNetEncoder = new JsonDotNetEncoder(Formatting.Indented);
            if (message is SCMessage)
                 Debug.LogWarning("<color=#779933>" + System.DateTime.Now + " -- " + message.GetType().ToString() + ": " + jsonDotNetEncoder.Encode(new List<object>(){message}) + "</color>");
            else
                Debug.LogWarning("<color=#0092cc>" + System.DateTime.Now + " -- " + message.GetType().ToString() + ": " + jsonDotNetEncoder.Encode(new List<object>(){message}) + "</color>");
 #endif
        }

        public static void Log(string message)
        {
            Debug.Log(message);
        }

        public static void Error(string message)
        {
            Debug.LogError(message);
        }

        public static void Warning(string message)
        {
            Debug.LogWarning(message);
        }
    }
}
