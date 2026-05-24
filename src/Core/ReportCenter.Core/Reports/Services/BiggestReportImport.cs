using System.IO.Compression;
using DocumentFormat.OpenXml.Spreadsheet;
using ReportCenter.Common.Providers.Storage.Interfaces;
using ReportCenter.Core.Reports.Interfaces;
using System.Xml.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using DocumentFormat.OpenXml;
using Microsoft.Extensions.FileProviders;

namespace ReportCenter.Core.Reports.Services;

public class BiggestReportImport : IBiggestReportImport
{
    private readonly IStorageService _storageService;
    private readonly string _tempPath;

    public BiggestReportImport(IStorageService storageService)
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "report-center", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);

        _storageService = storageService;
    }

    public async Task<BiggestReportImportStream> OpenReadStreamAsync(
        string fullFileName,
        CancellationToken cancellationToken = default)
    {
        var originalFilePath = Path.Combine(_tempPath, "originalFile.xlsx");

        await using (var writeFileStream = new FileStream(
            originalFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true))
        {
            await using (var readFileStream = await _storageService.OpenReadAsync(fullFileName, cancellationToken)
                ?? throw new FileNotFoundException($"File '{fullFileName}' was not found."))
            {
                await readFileStream.CopyToAsync(writeFileStream, cancellationToken);
            }
        }

        ZipFile.ExtractToDirectory(originalFilePath, _tempPath);
        File.Delete(originalFilePath);

        return new BiggestReportImportStream(_tempPath);
    }
}

public class BiggestReportImportStream : IAsyncDisposable
{
    private readonly string _tempPath;
    private readonly Workbook _workbook;
    private readonly XDocument _workbookXmlRels;
    private readonly SharedStringIndexer _sharedStringIndexer;
    static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public BiggestReportImportStream(string tempPath)
    {
        _tempPath = tempPath;
        var workbookXml = XDocument.Load(Path.Combine(_tempPath, "xl", "workbook.xml"));
        _workbook = new Workbook(workbookXml.Root!.ToString(SaveOptions.DisableFormatting));
        _workbookXmlRels = XDocument.Load(Path.Combine(_tempPath, "xl", "_rels", "workbook.xml.rels"));
        _sharedStringIndexer = new SharedStringIndexer();
    }

    public void PrintAvailableLinesToConsole()
    {
        Console.WriteLine("workbook.xml - sheets");
        var sheets = _workbook.Sheets?.Elements<Sheet>() ?? Enumerable.Empty<Sheet>();
        foreach (var sheet in sheets)
        {
            Console.WriteLine(
                $"Name={sheet.Name?.Value}, SheetId={sheet.SheetId?.Value}, Id={sheet.Id?.Value}, State={sheet.State?.Value}");
        }

        Console.WriteLine("workbook.xml.rels - relationships");
        foreach (var relationship in _workbookXmlRels.Root?.Elements() ?? Enumerable.Empty<XElement>())
        {
            Console.WriteLine(
                $"Id={relationship.Attribute("Id")?.Value}, Type={relationship.Attribute("Type")?.Value}, Target={relationship.Attribute("Target")?.Value}, TargetMode={relationship.Attribute("TargetMode")?.Value}");
        }
    }

    public async Task<BiggestReportImportSheet> OpenSheetAsync(string sheetName)
    {
        var stackSheet = new Stack<string>();
        await EnsureSharedStringsIndexedAsync();

        foreach (var sheet in ResolveSheets(sheetName))
            stackSheet.Push(sheet.Id!);

        if (stackSheet.Count <= 0)
            throw new KeyNotFoundException($"No Sheet found with name {sheetName}");

        return new BiggestReportImportSheet();
    }

    private async Task EnsureSharedStringsIndexedAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            await _sharedStringIndexer.BuildIndexAsync(Path.Combine(_tempPath, "xl", "sharedStrings.xml"));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private IEnumerable<Sheet> ResolveSheets(string sheetName)
    {
        var item = FindSheetByName(sheetName);
        if (item != null)
        {
            yield return item;
            yield break;
        }

        for (var index = 1; ; index++)
        {
            var newName = BuildWorksheetDisplayName(sheetName, index);
            item = FindSheetByName(newName);

            if (item == null)
                yield break;

            yield return item;
        }
    }

    private Sheet? FindSheetByName(string sheetName)
    {
        return _workbook.Sheets?.Elements<Sheet>()
            .FirstOrDefault(x => x.Name?.Value == sheetName);
    }

    private static string BuildWorksheetDisplayName(string sheetName, int sheetNameCounter)
    {
        var sheetBaseName = sheetName;
        var suffix = $" | {sheetNameCounter}";
        var maxBaseLength = Math.Max(1, 31 - suffix.Length);
        if (sheetBaseName.Length > maxBaseLength)
            sheetBaseName = sheetBaseName[..maxBaseLength];

        return SecurityElement.Escape($"{sheetBaseName}{suffix}");
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);

        _sharedStringIndexer.Dispose();

        return ValueTask.CompletedTask;
    }
}

public sealed class BiggestReportImportSheet : IAsyncDisposable
{
    public BiggestReportImportSheet()
    {
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
