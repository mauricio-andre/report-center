using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ReportCenter.Common.Providers.MessageQueues.Enums;
using ReportCenter.Core.Reports.ObjectValues;

namespace ReportCenter.Core.Reports.Entities;

public class Report
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public string Domain { get; set; } = string.Empty;
    [Required]
    public string Application { get; set; } = string.Empty;
    [Required]
    public ReportType ReportType { get; set; }
    [Required]
    public string DocumentName { get; set; } = string.Empty;
    [Required]
    public string DocumentKey { get; set; } = string.Empty;
    [Required]
    public short Version { get; set; } = 1;
    [Required]
    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.Now;
    [Required]
    public DateTimeOffset ExpirationDate { get; set; }
    [Required]
    public string UserIdentifier { get; set; } = string.Empty;
    [Required]
    public ProcessState ProcessState { get; set; } = ProcessState.Waiting;
    public FlexibleObject Filters { get; set; } = new FlexibleObject();
    public FlexibleObject ExtraProperties { get; set; } = new FlexibleObject();
    public TimeSpan? ProcessTimer { get; set; }
    public string? FileExtension { get; set; }

    [NotMapped]
    public string FullFileName
    {
        get
        => Path.Combine(
            Domain,
            Application,
            ReportType.ToString(),
            Version.ToString(),
            DocumentName,
            string.Concat(Id, ".", FileExtension));
    }
}
