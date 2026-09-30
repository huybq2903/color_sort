/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
*/

using Sirenix.Serialization;
using UnityEngine;

namespace Falcon.Helpers.UI
{
    public static class DeepCloneObject
    {
        public static T Clone<T>(T obj)
        {
            var bytes = SerializationUtility.SerializeValue(obj, DataFormat.Binary);
            return SerializationUtility.DeserializeValue<T>(bytes, DataFormat.Binary);
        }
    }
}
