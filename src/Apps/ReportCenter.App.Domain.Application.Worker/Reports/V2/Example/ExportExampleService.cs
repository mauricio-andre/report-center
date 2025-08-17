using ClosedXML.Excel;
using ReportCenter.App.GrpcServer.Methods.V1.Examples;
using ReportCenter.Common.Providers.Storage.Interfaces;
using ReportCenter.Core.Reports.Entities;
using ReportCenter.Core.Reports.Interfaces;

namespace ReportCenter.App.Domain.Application.Worker.Reports.V2.Example;

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

        using (MemoryStream memory = new())
        {
            using (var serverStreamingCall = _client.ExportList(filters, cancellationToken: cancellationToken))
            using (var xlWorkbook = new XLWorkbook())
            {
                IXLTable? table = null;
                var tempList = new List<WorksheetExampleDto>();
                var _sheet = xlWorkbook.Worksheets.Add(WorksheetExampleDto.WorksheetName);
                while (await serverStreamingCall.ResponseStream.MoveNext(cancellationToken))
                {
                    tempList.Add(new WorksheetExampleDto(
                        serverStreamingCall.ResponseStream.Current.Texto,
                        serverStreamingCall.ResponseStream.Current.Inteiro,
                        (decimal)serverStreamingCall.ResponseStream.Current.Decimal,
                        serverStreamingCall.ResponseStream.Current.Data.ToDateTimeOffset(),
                        report.ExtraProperties.Data["additionalProp1"].ToString()!));

                    if (tempList.Count < 100)
                        continue;

                    table = DataXLTable(table, tempList, _sheet);
                }

                DataXLTable(table, tempList, _sheet);
                cancellationToken.ThrowIfCancellationRequested();
                xlWorkbook.SaveAs(memory);
            }

            memory.Position = 0;
            cancellationToken.ThrowIfCancellationRequested();
            await _storageService.SaveAsync(report.FullFileName, memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", cancellationToken);
        }

    }

    private static IXLTable? DataXLTable(IXLTable? table, List<WorksheetExampleDto> tempList, IXLWorksheet _sheet)
    {
        if (tempList.Count != 0)
        {
            if (table == null)
                table = _sheet.FirstCell().InsertTable(tempList);
            else
                _ = table.AppendData(tempList);

            tempList.Clear();
        }

        return table;
    }
}
