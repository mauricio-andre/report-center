using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using ReportCenter.Core.Reports.ObjectValues;

namespace ReportCenter.MongoDB.Serializers;

public class FlexibleObjectSerializer : IBsonSerializer<FlexibleObject>
{
    public Type ValueType => typeof(FlexibleObject);

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, FlexibleObject value)
    {
        if (value?.Data == null)
        {
            context.Writer.WriteNull();
            return;
        }

        var bson = BsonDocument.Parse(value.ToJson());
        BsonSerializer.Serialize(context.Writer, bson);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        Serialize(context, args, (FlexibleObject)value);
    }

    public FlexibleObject Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        // Lê o documento BSON e converte para Dictionary<string, object>
        var dictionary = BsonSerializer.Deserialize<Dictionary<string, object>>(context.Reader);
        return new FlexibleObject(dictionary);
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }
}
