/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public static class JObjDictionaryExtensions
    {
        public static bool TryGetString(this IDictionary<string, JToken> dictionary, string key, out string value)
        {
            if (dictionary.TryGetValue(key, out var token))
            {
                value = token.ToString();
                return true;
            }
            value = null;
            return false;
        } 
        
        public static bool TryGetBool(this IDictionary<string, JToken> dictionary, string key, out bool value)
        {
            if (dictionary.TryGetValue(key, out var token))
            {
                if (bool.TryParse(token.ToString(), out value))
                {
                    return true;
                }
                value = false;
                return false;
            }
            value = false;
            return false;
        } 
        
        public static bool TryGetDouble(this IDictionary<string, JToken> dictionary, string key, out double value)
        {
            if (dictionary.TryGetValue(key, out var token))
            {
                if (double.TryParse(token.ToString(), out value))
                {
                    return true;
                }
                value = 0;
                return false;
            }
            value = 0;
            return false;
        } 
    }
}