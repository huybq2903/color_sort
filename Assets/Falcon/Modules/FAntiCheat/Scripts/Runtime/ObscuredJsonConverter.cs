/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-08-08

using System;
using CodeStage.AntiCheat.ObscuredTypes;
using Newtonsoft.Json;
namespace Falcon.Modules.FAntiCheat.Scripts.Runtime
{

// === Int ===
public sealed class ObscuredIntJsonConverter : JsonConverter<ObscuredInt>
{
    public override void WriteJson(JsonWriter w, ObscuredInt v, JsonSerializer s) => w.WriteValue((int)v);
    public override ObscuredInt ReadJson(JsonReader r, Type t, ObscuredInt e, bool h, JsonSerializer s)
    {
        if (r.TokenType == JsonToken.Null) return 0;
        if (r.TokenType == JsonToken.Integer || r.TokenType == JsonToken.Float) return Convert.ToInt32(r.Value);
        if (r.TokenType == JsonToken.String && int.TryParse((string)r.Value, out var n)) return n;
        throw new JsonSerializationException($"Cannot parse ObscuredInt from {r.TokenType}");
    }
}

// === Long ===
public sealed class ObscuredLongJsonConverter : JsonConverter<ObscuredLong>
{
    public override void WriteJson(JsonWriter w, ObscuredLong v, JsonSerializer s) => w.WriteValue((long)v);
    public override ObscuredLong ReadJson(JsonReader r, Type t, ObscuredLong e, bool h, JsonSerializer s)
    {
        if (r.TokenType == JsonToken.Null) return 0L;
        if (r.TokenType == JsonToken.Integer || r.TokenType == JsonToken.Float) return Convert.ToInt64(r.Value);
        if (r.TokenType == JsonToken.String && long.TryParse((string)r.Value, out var n)) return n;
        throw new JsonSerializationException($"Cannot parse ObscuredLong from {r.TokenType}");
    }
}

// === UInt ===
public sealed class ObscuredUIntJsonConverter : JsonConverter<ObscuredUInt>
{
    public override void WriteJson(JsonWriter w, ObscuredUInt v, JsonSerializer s) => w.WriteValue((uint)v);
    public override ObscuredUInt ReadJson(JsonReader r, Type t, ObscuredUInt e, bool h, JsonSerializer s)
    {
        if (r.TokenType == JsonToken.Null) return 0u;
        if (r.TokenType == JsonToken.Integer || r.TokenType == JsonToken.Float) return Convert.ToUInt32(r.Value);
        if (r.TokenType == JsonToken.String && uint.TryParse((string)r.Value, out var n)) return n;
        throw new JsonSerializationException($"Cannot parse ObscuredUInt from {r.TokenType}");
    }
}

// === Float ===
public sealed class ObscuredFloatJsonConverter : JsonConverter<ObscuredFloat>
{
    public override void WriteJson(JsonWriter w, ObscuredFloat v, JsonSerializer s) => w.WriteValue((float)v);
    public override ObscuredFloat ReadJson(JsonReader r, Type t, ObscuredFloat e, bool h, JsonSerializer s)
    {
        if (r.TokenType == JsonToken.Null) return 0f;
        if (r.TokenType == JsonToken.Integer || r.TokenType == JsonToken.Float) return Convert.ToSingle(r.Value);
        if (r.TokenType == JsonToken.String && float.TryParse((string)r.Value, out var n)) return n;
        throw new JsonSerializationException($"Cannot parse ObscuredFloat from {r.TokenType}");
    }
}

// === Double ===
public sealed class ObscuredDoubleJsonConverter : JsonConverter<ObscuredDouble>
{
    public override void WriteJson(JsonWriter w, ObscuredDouble v, JsonSerializer s) => w.WriteValue((double)v);
    public override ObscuredDouble ReadJson(JsonReader r, Type t, ObscuredDouble e, bool h, JsonSerializer s)
    {
        if (r.TokenType == JsonToken.Null) return 0d;
        if (r.TokenType == JsonToken.Integer || r.TokenType == JsonToken.Float) return Convert.ToDouble(r.Value);
        if (r.TokenType == JsonToken.String && double.TryParse((string)r.Value, out var n)) return n;
        throw new JsonSerializationException($"Cannot parse ObscuredDouble from {r.TokenType}");
    }
}

// === Bool ===
public sealed class ObscuredBoolJsonConverter : JsonConverter<ObscuredBool>
{
    public override void WriteJson(JsonWriter w, ObscuredBool v, JsonSerializer s) => w.WriteValue((bool)v);
    public override ObscuredBool ReadJson(JsonReader r, Type t, ObscuredBool e, bool h, JsonSerializer s)
    {
        if (r.TokenType == JsonToken.Null) return false;
        if (r.TokenType == JsonToken.Boolean) return (bool)r.Value;
        if (r.TokenType == JsonToken.Integer) return Convert.ToInt64(r.Value) != 0;
        if (r.TokenType == JsonToken.String && bool.TryParse((string)r.Value, out var b)) return b;
        throw new JsonSerializationException($"Cannot parse ObscuredBool from {r.TokenType}");
    }
}

// === String ===
public sealed class ObscuredStringJsonConverter : JsonConverter<ObscuredString>
{
    public override void WriteJson(JsonWriter w, ObscuredString v, JsonSerializer s)
    {
        // ACTk support null? An toàn: nếu v == null -> ghi null
        string raw = v; // implicit to string
        if (raw == null) { w.WriteNull(); return; }
        w.WriteValue(raw);
    }

    public override ObscuredString ReadJson(JsonReader r, Type t, ObscuredString e, bool h, JsonSerializer s)
    {
        if (r.TokenType == JsonToken.Null) return (string)null;
        if (r.TokenType == JsonToken.String) return (string)r.Value;
        // chấp nhận số -> ToString
        if (r.TokenType == JsonToken.Integer || r.TokenType == JsonToken.Float || r.TokenType == JsonToken.Boolean)
            return Convert.ToString(r.Value);
        throw new JsonSerializationException($"Cannot parse ObscuredString from {r.TokenType}");
    }
}
}