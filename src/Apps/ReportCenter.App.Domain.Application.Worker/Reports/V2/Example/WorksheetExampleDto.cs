using ClosedXML.Attributes;

namespace ReportCenter.App.Domain.Application.Worker.Reports.V2.Example;

public class WorksheetExampleDto(
    string Text,
    int Integer,
    decimal Double,
    DateTimeOffset Date,
    string Fixed)
{
    public static readonly string WorksheetName = "Relatórios";

    [XLColumn(Header = "Texto")]
    public string Text { get; } = Text;
    [XLColumn(Header = "Inteiro")]
    public int Integer { get; } = Integer;
    [XLColumn(Header = "Decimal")]
    public decimal Double { get; } = Double;
    [XLColumn(Header = "Data")]
    public DateTimeOffset Date { get; } = Date;
    [XLColumn(Header = "Propriedade Fixa")]
    public string Fixed { get; } = Fixed;
};
