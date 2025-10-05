using ClosedXML.Excel;
using ReportCenter.App.GrpcServer.Methods.V1.Examples;
using ReportCenter.Common.Providers.Storage.Interfaces;
using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Interfaces;

namespace ReportCenter.App.Domain.Application.Worker.Reports.V1.Example;

public class ExportExampleService : IReportService
{
    private readonly ILogger<ExportExampleService> _logger;
    private readonly IStorageService _storageService;
    private readonly ExamplesService.ExamplesServiceClient _client;

    public ExportExampleService(
        ILogger<ExportExampleService> logger,
        IStorageService storageService,
        ExamplesService.ExamplesServiceClient client)
    {
        _logger = logger;
        _storageService = storageService;
        _client = client;
    }

    public async Task HandleAsync(Report report, CancellationToken cancellationToken = default)
    {
        var filters = report.Filters.ToObject<ExampleExportRequest>();

        using (var stream = await _storageService.OpenWriteAsync(
            report.FullFileName,
            expiryDate: report.ExpirationDate,
            cancellationToken: cancellationToken))
        using (var serverStreamingCall = _client.ExportList(filters, cancellationToken: cancellationToken))
        using (var xlWorkbook = new XLWorkbook())
        {
            var _sheet = xlWorkbook.Worksheets.Add(WorksheetExampleDto.WorksheetName);

            _sheet.Cell(1, 1).Value = "Column 1 Header";
            _sheet.Cell(1, 2).Value = "Column 2 Header";
            _sheet.Cell(1, 3).Value = "Column 3 Header";
            _sheet.Cell(1, 4).Value = "Column 4 Header";
            _sheet.Cell(1, 5).Value = "Column 5 Header";

            var rowNumber = 2;
            while (await serverStreamingCall.ResponseStream.MoveNext(cancellationToken))
            {
                _sheet.Row(rowNumber).Cell(1).InsertData(new[] {
                    new WorksheetExampleDto(
                        serverStreamingCall.ResponseStream.Current.Texto,
                        serverStreamingCall.ResponseStream.Current.Inteiro,
                        (decimal)serverStreamingCall.ResponseStream.Current.Decimal,
                        serverStreamingCall.ResponseStream.Current.Data.ToDateTimeOffset(),
                        report.ExtraProperties.Data["additionalProp1"].ToString()!)
                });

                rowNumber++;
            }

            cancellationToken.ThrowIfCancellationRequested();
            xlWorkbook.SaveAs(stream);
        }
    }
}
