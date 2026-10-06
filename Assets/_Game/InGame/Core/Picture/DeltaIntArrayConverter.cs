using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Falcon.InGame.Core
{
    /// <summary>Lưu int[] thành {"d":[...]} dạng hiệu bậc 2 theo từng thành phần (stride 2 = cặp x,y), đọc lại đúng từng đơn vị; mảng tuyệt đối của file cũ vẫn đọc được.</summary>
    public class DeltaIntArrayConverter : JsonConverter
    {
        private readonly int _stride;

        public DeltaIntArrayConverter() : this(2) { }

        public DeltaIntArrayConverter(int stride) => _stride = Math.Max(1, stride);

        public override bool CanConvert(Type objectType) => objectType == typeof(int[]);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is not int[] a)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("d");
            writer.WriteStartArray();
            var prev = new int[_stride];
            var step = new int[_stride];
            for (var i = 0; i < a.Length; i++)
            {
                var c = i % _stride;
                var d = a[i] - prev[c];
                writer.WriteValue(d - step[c]);
                step[c] = d;
                prev[c] = a[i];
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            var list = new List<int>();
            var delta = false;
            if (reader.TokenType == JsonToken.StartObject)
            {
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    if (reader.TokenType != JsonToken.PropertyName) continue;
                    var name = (string)reader.Value;
                    reader.Read();
                    if (name == "d")
                    {
                        delta = true;
                        ReadInts(reader, list);
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
            }
            else
            {
                ReadInts(reader, list);
            }

            var a = list.ToArray();
            if (!delta) return a;

            var prev = new int[_stride];
            var step = new int[_stride];
            for (var i = 0; i < a.Length; i++)
            {
                var c = i % _stride;
                step[c] += a[i];
                prev[c] += step[c];
                a[i] = prev[c];
            }

            return a;
        }

        private static void ReadInts(JsonReader reader, List<int> list)
        {
            while (reader.Read() && reader.TokenType != JsonToken.EndArray) list.Add(Convert.ToInt32(reader.Value));
        }
    }
}
