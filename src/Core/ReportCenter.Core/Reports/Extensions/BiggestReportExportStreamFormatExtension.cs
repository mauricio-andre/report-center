using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using ReportCenter.Core.Reports.Services;

namespace ReportCenter.Core.Reports.Extensions;

public static class BiggestReportExportStreamFormatExtension
{
    public static UInt32Value AddDefaultExcelFormatData(this BiggestReportExportStream biggestReportExportStream)
    {
        return biggestReportExportStream.AddCellFormat(new CellFormat
        {
            NumberFormatId = 14,
            ApplyNumberFormat = true
        });
    }

    public static UInt32Value AddFormatCnpj(this BiggestReportExportStream biggestReportExportStream)
    {
        var numberFormatId = biggestReportExportStream.NextNumberFormatId();
        biggestReportExportStream.AddNumberingFormats(new NumberingFormat
        {
            NumberFormatId = numberFormatId,
            FormatCode = @"00"".""000"".""000""/""0000""-""00"
        });

        return biggestReportExportStream.AddCellFormat(new CellFormat
        {
            NumberFormatId = numberFormatId,
            ApplyNumberFormat = true
        });
    }
}
