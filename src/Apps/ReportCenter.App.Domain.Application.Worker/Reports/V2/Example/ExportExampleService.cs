using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using Google.Protobuf.WellKnownTypes;
using ReportCenter.App.GrpcServer.Methods.V1.Examples;
using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Interfaces;

namespace ReportCenter.App.Domain.Application.Worker.Reports.V2.Example;

public class ExportExampleService : IReportService
{
    private readonly ILogger<ExportExampleService> _logger;
    private readonly ExamplesService.ExamplesServiceClient _client;
    private readonly IBiggestReportExport _biggestReportExport;

    public ExportExampleService(
        ILogger<ExportExampleService> logger,
        ExamplesService.ExamplesServiceClient client,
        IBiggestReportExport biggestReportExport)
    {
        _logger = logger;
        _client = client;
        _biggestReportExport = biggestReportExport;
    }

    public async Task HandleAsync(Report report, CancellationToken cancellationToken = default)
    {
        var filters = report.Filters.ToObject<ExampleExportRequest>();

        await using (var stream = _biggestReportExport.OpenWriteStream(report.FullFileName, "example", cancellationToken: cancellationToken))
        {

            stream.SetHeader([
                new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue("Texto")
                },
                new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue("Inteiro")
                },
                new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue("Decimal")
                },
                new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue("Data")
                }
            ]);

            using (var serverStreamingCall = _client.ExportList(filters, cancellationToken: cancellationToken))
            {
                while (await serverStreamingCall.ResponseStream.MoveNext(cancellationToken))
                {
                    await stream.WriteRowAsync([
                        new Cell
                        {
                            DataType = CellValues.String,
                            CellValue = new CellValue(serverStreamingCall.ResponseStream.Current.Texto)
                        },
                        new Cell
                        {
                            DataType = CellValues.Number,
                            CellValue = new CellValue(serverStreamingCall.ResponseStream.Current.Inteiro)
                        },
                        new Cell
                        {
                            DataType = CellValues.Number,
                            CellValue = new CellValue(serverStreamingCall.ResponseStream.Current.Decimal)
                        },
                        new Cell
                        {
                            // TODO Revisar forma de imprimir data
                            CellValue = new CellValue(serverStreamingCall.ResponseStream.Current.Data.ToDateTime().ToOADate().ToString(CultureInfo.InvariantCulture)),
                            DataType = new EnumValue<CellValues>(CellValues.Number)
                        }
                    ]);
                }
            }
        }

    }
}
