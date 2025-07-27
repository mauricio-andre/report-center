using System.Text.Json;

namespace ReportCenter.Core.Reports.ObjectValues;

public class FlexibleObject
{
    public Dictionary<string, object> Data { get; set; } = new();

    public FlexibleObject()
    { }

    public FlexibleObject(Dictionary<string, object> data)
    {
        Data = data;
    }

    public static FlexibleObject FromObject<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;
        return new FlexibleObject(dict);
    }

    public T ToObject<T>()
    {
        var json = JsonSerializer.Serialize(Data);
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }

    public string ToJson() => JsonSerializer.Serialize(Data);

    public override string ToString() => ToJson();
}
