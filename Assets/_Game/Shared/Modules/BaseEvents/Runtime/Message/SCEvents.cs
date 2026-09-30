/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-10
 */

using System;
using Falcon.Modules.Core.Network;
using Falcon.Shared.Common;
using Falcon.Shared.Common.Time;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Falcon.Shared.BaseEvents
{
    [FAMessage("sc_events")]
    public class SCEvents : SCMessage
    {
        public EventInfo[] events;
        public override void OnData()
        {
            WrapperTime.OverrideTime(timeServer);
            foreach (var @event in events)
            {
                var eventType = EventsRegister.GetType(@event.key);
                if (eventType == null) continue;
                if (Center.GetOrCreate(eventType) is IWrapperEvent eventWrapper)
                    eventWrapper.SyncFromServer(@event.config, @event.userData);
            }
        }
    }

    [JsonConverter(typeof(EventInfoConverter))]
    public class EventInfo
    {
        public string key;
        public ABaseEventConfig config;
        public ABaseEventUserData userData;
    }

    public class EventInfoConverter : JsonConverter<EventInfo>
    {
        public override void WriteJson(JsonWriter writer, EventInfo value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("key");
            writer.WriteValue(value.key);

            writer.WritePropertyName("config");
            serializer.Serialize(writer, value.config);

            writer.WritePropertyName("userData");
            serializer.Serialize(writer, value.userData);

            writer.WriteEndObject();
        }

        public override EventInfo ReadJson(JsonReader reader, Type objectType, EventInfo existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            var result = new EventInfo
            {
                key = (string)obj["key"]
            };

            var (typeConfig, typeData) = EventsRegister.GetConfigAndDataType(result.key);
            if (obj["config"]?.ToObject(typeConfig, serializer) is ABaseEventConfig config)
                result.config = config;
            if (obj["userData"]?.ToObject(typeData, serializer) is ABaseEventUserData userData)
                result.userData = userData;

            return result;
        }
    }
}