/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-10
 */

using System;
using CodeStage.AntiCheat.ObscuredTypes;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Shared.BaseEvents
{
    [CreateAssetMenu(fileName = "SO_Config_Event_", menuName = "Event/Config")]
    public class SOEventConfig : SerializedScriptableObject
    {
        public ABaseEventConfig configSO;

        [Button("Copy Config JSON")]
        private void CopyConfigJson()
        {
            if (configSO == null)
            {
                Debug.LogWarning($"[{nameof(SOEventConfig)}] configSO is null, cannot copy JSON.", this);
                return;
            }

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            settings.Converters.Add(new ObscuredIntJsonConverter());
            settings.Converters.Add(new ObscuredFloatJsonConverter());
            settings.Converters.Add(new ObscuredStringJsonConverter());

            GUIUtility.systemCopyBuffer = JsonConvert.SerializeObject(configSO, settings);
            Debug.Log($"[{nameof(SOEventConfig)}] Copied config JSON for {configSO.GetType().Name}.", this);
        }

        private sealed class ObscuredIntJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) =>
                objectType == typeof(ObscuredInt) || objectType == typeof(ObscuredInt?);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value != null) writer.WriteValue((ObscuredInt)value);
            }
        }

        private sealed class ObscuredStringJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(ObscuredString);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue((string)(ObscuredString)value);
            }
        }

        private sealed class ObscuredFloatJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) =>
                objectType == typeof(ObscuredFloat) || objectType == typeof(ObscuredFloat?);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value != null) writer.WriteValue((ObscuredFloat)value);
            }
        }
    }
}
