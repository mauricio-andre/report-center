using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using ReportCenter.Core.Data;
using ReportCenter.MongoDB.Data;
using ReportCenter.MongoDB.Serializers;

namespace ReportCenter.Mongo.Extensions;

public static class MongoServiceCollectionExtensions
{
    public static IServiceCollection AddMongoCoreDbContext(
        this IServiceCollection services,
        string connectionString,
        string databaseName)
    {
        BsonSerializer.RegisterSerializer(new FlexibleObjectSerializer());
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        return services
            .AddScoped<CoreDbContext, MongoCoreDbContext>()
            .AddDbContextFactory<CoreDbContext, MongoCoreDbContextFactory>(
                options => options.UseMongoDB(connectionString, databaseName),
                ServiceLifetime.Scoped)
            .AddSingleton<IMongoDatabase>(x => new MongoClient(connectionString).GetDatabase(databaseName));
    }
}
