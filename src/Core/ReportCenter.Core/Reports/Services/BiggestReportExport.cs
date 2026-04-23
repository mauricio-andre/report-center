using System.IO.Compression;
using System.Security;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using ReportCenter.Common.Exceptions;
using ReportCenter.Common.Providers.Storage.Interfaces;
using ReportCenter.Core.Reports.Interfaces;

namespace ReportCenter.Core.Reports.Services;

public class BiggestReportExport : IBiggestReportExport
{
    private readonly IStorageService _storageService;

    public BiggestReportExport(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public BiggestReportExportStream OpenWriteStream(
        string fullFileName,
        string sheetBaseName,
        DateTimeOffset expirationDate,
        int? maxRows = null,
        CancellationToken cancellationToken = default)
    {
        return new BiggestReportExportStream(
            _storageService,
            fullFileName,
            sheetBaseName,
            maxRows,
            expirationDate,
            cancellationToken);
    }
}

public class BiggestReportExportStream : IAsyncDisposable
{
    private readonly IStorageService _storageService;
    private readonly string _fullFileName;
    private readonly string _tempPath;
    private readonly DateTimeOffset _expirationDate;
    private readonly CancellationToken _cancellationToken;
    private readonly Stylesheet _stylesheet = new Stylesheet(
        new NumberingFormats(),
        new Fonts((IEnumerable<Font>)[new Font()]),
        new Fills((IEnumerable<Fill>)[new Fill()]),
        new Borders((IEnumerable<Border>)[new Border()]),
        new CellFormats((IEnumerable<CellFormat>)[new CellFormat()])
    );
    private readonly Dictionary<string, Stack<BiggestReportExportSheet>> _sheetStreamDictionary = new();
    private BiggestReportExportSheet _currentSheetStream;

    public BiggestReportExportStream(
        IStorageService storageService,
        string fullFileName,
        string sheetBaseName,
        int? maxRows,
        DateTimeOffset expirationDate,
        CancellationToken cancellationToken)
    {
        _storageService = storageService;
        _fullFileName = fullFileName;
        _cancellationToken = cancellationToken;
        _expirationDate = expirationDate;

        _tempPath = Path.Combine(Path.GetTempPath(), "report-center", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);

        _currentSheetStream = new BiggestReportExportSheet(
            sheetBaseName,
            _tempPath,
            maxRows,
            _cancellationToken);

        var stackSheet = new Stack<BiggestReportExportSheet>();
        stackSheet.Push(_currentSheetStream);
        _sheetStreamDictionary.Add(_currentSheetStream.GetSheetBaseName(), stackSheet);
    }

    public BiggestReportExportStream EnsureSheet(string sheetBaseName, int? maxRows = null)
    {
        if (_sheetStreamDictionary.TryGetValue(sheetBaseName.Trim(), out var stackSheetCurrent))
        {
            _currentSheetStream = stackSheetCurrent.Peek();
            return this;
        }

        _currentSheetStream = new BiggestReportExportSheet(
            sheetBaseName,
            _tempPath,
            maxRows,
            _cancellationToken);

        var stackSheet = new Stack<BiggestReportExportSheet>();
        stackSheet.Push(_currentSheetStream);
        _sheetStreamDictionary.Add(_currentSheetStream.GetSheetBaseName(), stackSheet);
        return this;
    }

    public UInt32Value NextNumberFormatId()
    {
        var currentMax = _stylesheet.NumberingFormats!.ChildElements
            .Select(x => ((NumberingFormat)x).NumberFormatId)
            .Max();

        if (currentMax == null || currentMax <= 164)
            return 165;

        return currentMax + 1;
    }

    public UInt32Value AddNumberingFormats(NumberingFormat numberingFormat)
    {
        if (!numberingFormat.NumberFormatId!.HasValue)
            numberingFormat.NumberFormatId = NextNumberFormatId();

        if (numberingFormat.NumberFormatId! <= 164)
            throw new ArgumentOutOfRangeException(
                "numberingFormat.NumberFormatId",
                $"Values lower than or equal to 164 are reserved by Excel.");

        NumberingFormat[] safeMatch = [numberingFormat];
        _stylesheet.NumberingFormats!.Append(safeMatch);

        return numberingFormat.NumberFormatId;
    }

    public UInt32Value AddCellFormat(CellFormat cellFormat)
    {
        CellFormat[] safeMatch = [cellFormat];
        _stylesheet.CellFormats!.Append(safeMatch);
        return (uint)(_stylesheet.CellFormats!.ChildElements.Count - 1);
    }

    public void SetHeader(Cell[]? cells) => _currentSheetStream.SetHeader(cells);

    public Task WriteRowAsync(Cell[] cells)
    {
        if (!_currentSheetStream.IsReachedMaxRows)
            return _currentSheetStream.WriteRowAsync(cells);

        var sheetName = _currentSheetStream.GetSheetBaseName();
        _currentSheetStream = _currentSheetStream.CloneSheet();
        _sheetStreamDictionary[sheetName].Push(_currentSheetStream);
        return _currentSheetStream.WriteRowAsync(cells);
    }

    public async Task SaveAsync()
    {
        using (var stream = await _storageService.OpenWriteAsync(
            _fullFileName,
            expirationDate: _expirationDate,
            cancellationToken: _cancellationToken))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false))
        {
            var index = 0;
            var orderedSheetDictionary = _sheetStreamDictionary.Values
                .SelectMany(stackSheet => stackSheet.Reverse())
                .ToDictionary(sheet => index += 1);

            WriteContentType(zip, orderedSheetDictionary);
            WriteRels(zip);
            WriteWorkbookXml(zip, orderedSheetDictionary);
            WriteWorkbookXmlRels(zip, orderedSheetDictionary);
            // xl/styles.xml
            AddXmlFromString(zip, "xl/styles.xml", _stylesheet.OuterXml);

            // xl/worksheets/sheet{i}.xml
            foreach (var item in orderedSheetDictionary)
            {
                if (!item.Value.IsSheetOpen && !item.Value.IsReachedMaxRows)
                    await item.Value.OpenSheet();

                await item.Value.CloseSheet();

                var entry = zip.CreateEntry($"xl/worksheets/sheet{item.Key}.xml", CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                using var sourceStream = new FileStream(item.Value.GetTempFullFileName(), FileMode.Open, FileAccess.Read, FileShare.Read);
                await sourceStream.CopyToAsync(entryStream, _cancellationToken);
            }
        }
    }

    private static void WriteContentType(ZipArchive zip, Dictionary<int, BiggestReportExportSheet> sheetDictionary)
    {
        // [Content_Types].xml
        var contentTypesStringBuilder = new StringBuilder();
        foreach (var key in sheetDictionary.Keys)
        {
            contentTypesStringBuilder
                .Append(@$"<Override PartName=""/xl/worksheets/sheet{key}.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>");
        }

        AddXmlFromString(zip, "[Content_Types].xml",
            @$"<?xml version=""1.0"" encoding=""UTF-8""?>
            <Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
                <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
                <Default Extension=""xml"" ContentType=""application/xml""/>
                <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
                <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>
                {contentTypesStringBuilder}
            </Types>");
    }

    private static void WriteRels(ZipArchive zip) =>
        // _rels/.rels
        AddXmlFromString(zip, "_rels/.rels",
            @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
                <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
            </Relationships>");

    private void WriteWorkbookXml(ZipArchive zip, Dictionary<int, BiggestReportExportSheet> sheetDictionary)
    {
        // xl/workbook.xml
        var workbook = new StringBuilder();
        var sheetNameCounter = new Dictionary<string, int>();
        foreach (var item in sheetDictionary)
        {
            var sheetBaseName = item.Value.GetSheetBaseName();

            sheetNameCounter.TryAdd(sheetBaseName, 0);
            sheetNameCounter[sheetBaseName] = sheetNameCounter[sheetBaseName] + 1;

            var sheetName = item.Value.BuildWorksheetDisplayName(
                sheetNameCounter[sheetBaseName],
                _sheetStreamDictionary[sheetBaseName].Count);

            workbook
                .Append(@$"<sheet name=""{sheetName}"" sheetId=""{item.Key}"" r:id=""rId{item.Key}""/>");
        }

        AddXmlFromString(zip, "xl/workbook.xml",
            @$"<?xml version=""1.0"" encoding=""UTF-8""?>
            <workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
            <sheets>
                {workbook}
            </sheets>
            </workbook>");
    }

    private static void WriteWorkbookXmlRels(ZipArchive zip, Dictionary<int, BiggestReportExportSheet> sheetDictionary)
    {
        // xl/_rels/workbook.xml.rels
        var workbookRels = new StringBuilder();
        foreach (var key in sheetDictionary.Keys)
        {
            workbookRels
                .Append(@$"<Relationship Id=""rId{key}"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet{key}.xml""/>");
        }

        var maxKeys = sheetDictionary.Keys.Max();

        AddXmlFromString(zip, "xl/_rels/workbook.xml.rels",
            @$"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
            <Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
                {workbookRels}
                <Relationship Id=""rId{maxKeys + 1}"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>
            </Relationships>");
    }

    private static void AddXmlFromString(ZipArchive zip, string entryName, string xmlContent)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(xmlContent);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sheet in _sheetStreamDictionary.Values.SelectMany(stackSheet => stackSheet.ToList()))
        {
            var fullFileName = sheet.GetTempFullFileName();
            await sheet.DisposeAsync();

            if (!Directory.Exists(_tempPath))
                continue;

            File.Delete(fullFileName);
        }

        Directory.Delete(_tempPath);
    }
}

public sealed class BiggestReportExportSheet : IAsyncDisposable
{
    private static readonly char[] InvalidSheetNameChars = [':', '\\', '/', '?', '*', '[', ']'];
    private const int DEFAULT_MAX_ROWS_PER_SHEET = 1_000_000;
    private const int MAX_COLUMNS_PER_SHEET = 16_384;
    private readonly string _tempFullFileName;
    private readonly string _sheetBaseName;
    private readonly int _maxRows;
    private readonly CancellationToken _cancellationToken;
    private Cell[]? _headerCells;
    private uint _currentRow = 1;
    private FileStream? _fileStream;
    private StreamWriter? _streamWriter;
    public bool IsSheetOpen { get; private set; }
    public bool IsReachedMaxRows { get; private set; }

    public BiggestReportExportSheet(
        string sheetBaseName,
        string tempPath,
        int? maxRows = DEFAULT_MAX_ROWS_PER_SHEET,
        CancellationToken cancellationToken = default)
    {
        ValidateSheetBaseName(sheetBaseName);

        _sheetBaseName = sheetBaseName.Trim();
        _maxRows = maxRows > DEFAULT_MAX_ROWS_PER_SHEET || maxRows < 2
            ? DEFAULT_MAX_ROWS_PER_SHEET
            : maxRows ?? DEFAULT_MAX_ROWS_PER_SHEET;

        _cancellationToken = cancellationToken;
        _tempFullFileName = Path.Combine(tempPath, $"{Guid.NewGuid()}.xml");
    }

    private static void ValidateSheetBaseName(string sheetBaseName)
    {
        if (string.IsNullOrEmpty(sheetBaseName.Trim()))
            throw new ArgumentOutOfRangeException(
                "sheetBaseName",
                "Excel sheet names cannot be empty.");

        if (sheetBaseName.Trim().Length > 31)
            throw new ArgumentOutOfRangeException(
                "sheetBaseName",
                "Excel sheet names cannot exceed 31 characters.");

        if (InvalidSheetNameChars.Any(item => sheetBaseName.Contains(item)))
            throw new ArgumentOutOfRangeException(
                "sheetBaseName",
                $"Excel tab names cannot contain the following special characters ({string.Join(",", InvalidSheetNameChars)}).");
    }


    public string GetTempFullFileName() => _tempFullFileName;

    public string GetSheetBaseName() => _sheetBaseName;

    public string BuildWorksheetDisplayName(int sheetNameCounter = 0, int sheetNameTotal = 1)
    {
        if (sheetNameTotal <= 1)
            return SecurityElement.Escape(_sheetBaseName);

        var sheetBaseName = _sheetBaseName;
        var suffix = $" | {sheetNameCounter}";
        var maxBaseLength = Math.Max(1, 31 - suffix.Length);
        if (sheetBaseName.Length > maxBaseLength)
            sheetBaseName = sheetBaseName[..maxBaseLength];

        return SecurityElement.Escape($"{sheetBaseName}{suffix}");
    }

    public void SetHeader(Cell[]? cells) => _headerCells = cells;

    public BiggestReportExportSheet CloneSheet()
    {
        var sheet = new BiggestReportExportSheet(
            _sheetBaseName,
            Path.GetDirectoryName(_tempFullFileName)!,
            _maxRows,
            _cancellationToken);

        sheet.SetHeader(_headerCells);

        return sheet;
    }

    public async Task WriteRowAsync(Cell[] cells)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (IsReachedMaxRows)
            throw new ReportExportReachedMaxRowsException();

        if (cells.Length > MAX_COLUMNS_PER_SHEET)
            throw new ArgumentOutOfRangeException(
                "cells.Length",
                "Sheet exceeded the maximum number of columns allowed by Excel.");

        if (!IsSheetOpen)
            await OpenSheet();

        var row = new Row { RowIndex = _currentRow };
        for (uint index = 0; index < cells.Length; index++)
        {
            cells[index].CellReference = GetColumnName(index + 1) + _currentRow;
            Cell[] safeMatch = [cells[index]];
            row.Append(safeMatch);
        }

        _currentRow++;
        await _streamWriter!.WriteLineAsync(row.OuterXml);

        if (_currentRow == _maxRows + 1)
        {
            IsReachedMaxRows = true;
            await CloseSheet();
        }
    }

    public async Task OpenSheet()
    {
        if (IsSheetOpen)
            return;

        _currentRow = 1;
        IsSheetOpen = true;

        _fileStream = new FileStream(
            _tempFullFileName,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        _streamWriter = new StreamWriter(
            _fileStream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        await _streamWriter.WriteLineAsync(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
        await _streamWriter.WriteLineAsync(@"<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");
        await _streamWriter.WriteLineAsync(@"  <sheetData>");

        if (_headerCells != null && _headerCells.Length > 0)
            await WriteRowAsync(_headerCells.Select(header => (Cell)header.CloneNode(true)).ToArray());
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

    public async Task CloseSheet()
    {
        if (!IsSheetOpen)
            return;

        await _streamWriter!.WriteLineAsync(@"  </sheetData>");
        await _streamWriter!.WriteLineAsync(@"</worksheet>");

        await _streamWriter!.DisposeAsync();
        await _fileStream!.DisposeAsync();

        IsSheetOpen = false;
    }

    public async ValueTask DisposeAsync()
    {
        if (IsSheetOpen)
        {
            await _streamWriter!.DisposeAsync();
            await _fileStream!.DisposeAsync();
            IsSheetOpen = false;
        }
    }
}
