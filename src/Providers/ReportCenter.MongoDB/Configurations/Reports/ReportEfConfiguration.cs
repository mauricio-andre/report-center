using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;
using ReportCenter.Core.Reports.Entities;

namespace ReportCenter.MongoDB.Configurations.Reports;

public class ReportEfConfiguration : IEntityTypeConfiguration<Report>
{
    public static string CollectionName = "reports";
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToCollection(CollectionName);

        builder.HasKey(export => export.Id);

        // https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities#limitations
        // https://devblogs.microsoft.com/dotnet/efcore-mongodb/
        // https://www.mongodb.com/community/forums/t/working-with-bsondocument-on-mongodb-entityframeworkcore/285431/3
        // builder.OwnsOne(export => export.Filters, config =>
        // {
        //     // config
        //     // .Property(filters => filters.Data)
        //     // .HasConversion(
        //     //     v => JsonSerializer.Serialize(v, new JsonSerializerOptions() { WriteIndented = false }),
        //     //     v => JsonSerializer.Deserialize<Dictionary<string, object>>(v!, new JsonSerializerOptions())!)
        //     // .HasBsonRepresentation(BsonType.Document);
        // });

        // builder.OwnsOne(export => export.ExtraProperties, config =>
        // {
        //     // config
        //     // .Property(filters => filters.Data)
        //     // .HasConversion(
        //     //     v => JsonSerializer.Serialize(v, new JsonSerializerOptions() { WriteIndented = false }),
        //     //     v => JsonSerializer.Deserialize<Dictionary<string, object>>(v!, new JsonSerializerOptions())!)
        //     // .HasBsonRepresentation(BsonType.Document);
        // });

        builder.Ignore(report => report.Filters);
        builder.Ignore(report => report.ExtraProperties);
    }
}
