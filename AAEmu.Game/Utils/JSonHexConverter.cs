using System;
using Newtonsoft.Json;

namespace AAEmu.Game.Utils;

// Adapted from Source: https://stackoverflow.com/questions/70171426/c-sharp-json-converting-hex-literal-string-to-int

public class JSonHexConverterUInt : JsonConverter<uint>
{
    public override void WriteJson(JsonWriter writer, uint value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override uint ReadJson(JsonReader reader, Type objectType, uint existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var value = reader.Value as string;
        return Convert.ToUInt32(value, 16);
    }
}

public class JSonHexConverterULong : JsonConverter<ulong>
{
    public override void WriteJson(JsonWriter writer, ulong value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override ulong ReadJson(JsonReader reader, Type objectType, ulong existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var value = reader.Value as string;
        return Convert.ToUInt64(value, 16);
    }
}
