using DocumentFormat.OpenXml.Spreadsheet;
using ReportCenter.App.GrpcServer.Methods.V1.Examples;
using ReportCenter.Common.Providers.OAuth.Interfaces;
using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Interfaces;
using ReportCenter.Core.Reports.Extensions;

namespace ReportCenter.App.Domain.Application.Worker.Reports.V2.Example;

public class ExportExampleService : IReportService
{
    private readonly IOAuthTokenService _oAuthTokenService;
    private readonly ExamplesService.ExamplesServiceClient _client;
    private readonly IBiggestReportExport _biggestReportExport;

    public ExportExampleService(
        IOAuthTokenService oAuthTokenService,
        ExamplesService.ExamplesServiceClient client,
        IBiggestReportExport biggestReportExport)
    {
        _oAuthTokenService = oAuthTokenService;
        _client = client;
        _biggestReportExport = biggestReportExport;
    }

    public async Task HandleAsync(Report report, CancellationToken cancellationToken = default)
    {
        var filters = report.Filters.ToObject<ExampleExportRequest>();

        var token = await _oAuthTokenService.GetOAuthTokenAsync();
        var headers = new Grpc.Core.Metadata
        {
            { "Authorization", $"Bearer {token}" }
        };

        await using (var stream = _biggestReportExport.OpenWriteStream(
            report.FullFileName,
            "example",
            expirationDate: report.ExpirationDate,
            cancellationToken: cancellationToken))
        {
            var DataStyleIndex = stream.AddDefaultExcelFormatData();

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

            using (var serverStreamingCall = _client.ExportList(filters, headers, cancellationToken: cancellationToken))
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
                            CellValue = new CellValue(serverStreamingCall.ResponseStream.Current.Data.ToDateTime().ToOADate()),
                            StyleIndex = DataStyleIndex
                        }
                    ]);
                }

                await stream.SaveAsync();
            }
        }
    }
}
