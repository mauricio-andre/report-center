using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ReportCenter.App.RestServer.Endpoints.V1.Reports.Dtos;

public class CreateReportImportDto()
{
    [Required]
    public string Domain { get; set; } = string.Empty;
    [Required]
    public string Application { get; set; } = string.Empty;
    [Required]
    public string DocumentName { get; set; } = string.Empty;
    [Required]
    public string DocumentKey { get; set; } = string.Empty;
    [Required]
    public short Version { get; set; }
    [Required]
    public DateTimeOffset ExpirationDate { get; set; }
    public string? Filters { get; set; }
    public string? ExtraProperties { get; set; }
    [Required]
    public bool ExternalProcess { get; set; } = false;

    internal Dictionary<string, object>? FiltersDictionary
    {
        get => string.IsNullOrEmpty(Filters)
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, object>>(Filters);
    }

    internal Dictionary<string, object>? ExtraPropertiesDictionary {
        get => string.IsNullOrEmpty(ExtraProperties)
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, object>>(ExtraProperties);
    }

    internal Dictionary<string, string[]> CatchJsonFormatExceptions()
    {
        Dictionary<string, string[]> errors = new();

        try
        {
            var _ = FiltersDictionary;
        }
        catch
        {
            errors.Add(nameof(CreateReportImportDto.Filters), [Filters!]);
        }

        try
        {
            var _ = ExtraPropertiesDictionary;
        }
        catch
        {
            errors.Add(nameof(CreateReportImportDto.ExtraProperties), [ExtraProperties!]);
        }

        return errors;
    }
}
