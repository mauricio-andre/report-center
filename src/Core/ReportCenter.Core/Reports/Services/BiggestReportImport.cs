using System.IO.Compression;
using DocumentFormat.OpenXml.Spreadsheet;
using ReportCenter.Common.Providers.Storage.Interfaces;
using ReportCenter.Core.Reports.Interfaces;
using System.Xml.Linq;
using System.Security;
using DocumentFormat.OpenXml;
using System.Xml;

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

    public async Task<BiggestReportImportSheet> OpenSheetAsync(string sheetName)
    {
        var sheetPathDictionary = new Dictionary<string, string>();
        await EnsureSharedStringsIndexedAsync();

        foreach (var sheet in ResolveSheets(sheetName))
        {
            var sheetPath = GetSheetPathById(sheet.Id!);
            if (string.IsNullOrEmpty(sheetPath))
                throw new KeyNotFoundException($"No Rel Sheet found with ID {sheet.Id!}");
            if (!File.Exists(Path.Combine(_tempPath, "xl", sheetPath)))
                throw new FileNotFoundException(Path.Combine("xl", sheetPath));

            sheetPathDictionary.Add(sheet.Id!, Path.Combine(_tempPath, "xl", sheetPath));
        }

        if (sheetPathDictionary.Count <= 0)
            throw new KeyNotFoundException($"No Sheet found with name {sheetName}");

        return new BiggestReportImportSheet(sheetPathDictionary, _sharedStringIndexer);
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

    private string? GetSheetPathById(StringValue sheetId)
        =>_workbookXmlRels.Root?
            .Elements()
            .Where(element => element.Attribute("Id")?.Value == sheetId)
            .Select(element => element.Attribute("Target")?.Value)
            .FirstOrDefault();

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
    private readonly Dictionary<string, string> _sheetPathDictionary;
    private readonly SharedStringIndexer _sharedStringIndexer;
    public BiggestReportImportSheet(
        Dictionary<string, string> sheetPathDictionary,
        SharedStringIndexer sharedStringIndexer)
    {
        _sheetPathDictionary = sheetPathDictionary;
        _sharedStringIndexer = sharedStringIndexer;
    }

    public async IAsyncEnumerable<Cell[]> ReadLineAsync(int? cellsCount = null)
    {
        if (cellsCount is <= 0)
            throw new ArgumentOutOfRangeException(nameof(cellsCount), "Value must be null or greater than 0.");

        foreach (var fullFilePath in _sheetPathDictionary.Values)
        {
            await using (Stream stream = new FileStream(
                fullFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                useAsync: true
            ))
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
                DtdProcessing = DtdProcessing.Ignore,
                Async = true
            }))
            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "row")
                {
                    var cells = await ReadRowAsync(reader, cellsCount);
                    if (cells.Length > 0 && cells.Any(cell => !string.IsNullOrEmpty(cell.InnerText)))
                        yield return cells;
                }
            }
        }
    }

    private async Task<Cell[]> ReadRowAsync(XmlReader reader, int? cellsCount)
    {
        var cellList = new List<Cell>();
        var rowReference = reader.GetAttribute("r");

        using (var subtree = reader.ReadSubtree())
        {
            subtree.MoveToContent();
            while (await subtree.ReadAsync())
            {
                if (subtree.NodeType == XmlNodeType.Element && subtree.Name == "c")
                {
                    cellList.Add(await ReadCellAsync(subtree));
                }
            }
        }

        if (cellsCount is null)
            return cellList
                .OrderBy(cell => GetColumnIndex(cell.CellReference?.Value ?? ""))
                .ToArray();

        var cells = new Cell[cellsCount.Value];
        for (int index = 0; index < cells.Length; index++)
        {
            var columnName = GetColumnName((uint)(index + 1));
            var cell = cellList.FirstOrDefault(cell => cell.CellReference?.Value == columnName + rowReference);
            if (cell == null)
                cell = CreateEmptyCell(GetColumnName((uint)(index + 1)), rowReference);

            cells[index] = cell;
        }

        return cells;
    }

    private async Task<Cell> ReadCellAsync(XmlReader cellReader)
    {
        var cell = new Cell()
        {
            CellReference = cellReader.GetAttribute("r") ?? "",
            DataType = GetCellValues(cellReader)
        };

        using (var subtree = cellReader.ReadSubtree())
        {
            subtree.MoveToContent();
            while (await subtree.ReadAsync())
            {
                if (subtree.NodeType == XmlNodeType.Element && subtree.Name == "v")
                {
                    string raw = await subtree.ReadElementContentAsStringAsync();
                    cell.CellValue = new CellValue(await ResolveCellValueAsync(raw, cell.DataType));
                }
            }
        }

        return cell;
    }

    private static CellValues? GetCellValues(XmlReader cellReader)
        => cellReader.GetAttribute("t") switch
        {
            "s" => CellValues.SharedString,
            "str" => CellValues.String,
            "inlineStr" => CellValues.InlineString,
            "b" => CellValues.Boolean,
            "e" => CellValues.Error,
            "d" => CellValues.Date,
            "n" => CellValues.Number,
            _ => null
        };


    private async Task<string> ResolveCellValueAsync(string raw, EnumValue<CellValues>? dataType)
    {
        if ((dataType?.HasValue ?? false) && dataType.Value == CellValues.SharedString)
        {
            if (int.TryParse(raw, out int sharedIndex))
                return await _sharedStringIndexer.GetByIndexAsync(sharedIndex);

            return "";
        }

        return raw;
    }

    private static Cell CreateEmptyCell(string columnName, string? rowReference)
    {
        return new Cell
        {
            CellReference = string.IsNullOrEmpty(rowReference) ? "" : columnName + rowReference,
            DataType = null,
            CellValue = new CellValue(string.Empty)
        };
    }

    private static int GetColumnIndex(string cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
            return 0;

        var columnIndex = 0;
        foreach (var character in cellReference)
        {
            if (!char.IsLetter(character))
                break;

            columnIndex = (columnIndex * 26) + (char.ToUpperInvariant(character) - 'A' + 1);
        }

        return columnIndex;
    }

    private static string GetColumnName(uint columnIndex)
    {
        // O Excel tem no máximo 16384 colunas (XFD),
        // logo, o tamanho máximo do nome é 3 caracteres.
        Span<char> buffer = stackalloc char[3];
        int position = buffer.Length;

        while (columnIndex > 0)
        {
            columnIndex--; // ajuste: Excel começa em 1
            int remainder = (int)columnIndex % 26;
            buffer[--position] = (char)('A' + remainder);
            columnIndex /= 26;
        }

        return new string(buffer.Slice(position));
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
