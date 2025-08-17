using System.Text;
using ReportCenter.Common.Providers.Storage.Interfaces;
using DocumentFormat.OpenXml.Spreadsheet;
using System.IO.Compression;
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
        int maxRowsPerSheet = 1_000_000,
        CancellationToken cancellationToken = default)
    {
        maxRowsPerSheet = maxRowsPerSheet > 1_000_000
            ? 1_000_000
            : maxRowsPerSheet;

        return new BiggestReportExportStream(
            _storageService,
            fullFileName,
            sheetBaseName,
            maxRowsPerSheet,
            cancellationToken);
    }
}

public class BiggestReportExportStream : IAsyncDisposable
{
    private const string tempPath = "tmp"; // TODO Trocar para /tmp do SO
    private readonly IStorageService _storageService;
    private readonly string _fullFileName;
    private readonly string _sheetBaseName;
    private readonly int _maxRowsPerSheet;
    private CancellationToken _cancellationToken;
    private Cell[]? _headerCells;
    private int _totalSheets = 0;
    private uint _currentRow = 1;
    private bool _isSheetOpen = false;
    private FileStream? _fileStream = null;
    private StreamWriter? _streamWriter = null;

    public BiggestReportExportStream(
        IStorageService storageService,
        string fullFileName,
        string sheetBaseName,
        int maxRowsPerSheet,
        CancellationToken cancellationToken)
    {
        _storageService = storageService;
        _fullFileName = fullFileName;
        _sheetBaseName = sheetBaseName;
        _maxRowsPerSheet = maxRowsPerSheet;
        _cancellationToken = cancellationToken;
    }

    public void SetHeader(Cell[]? cells)
    {
        _headerCells = cells;
    }

    public async Task WriteRowAsync(Cell[] cells)
    {
        if (!_isSheetOpen)
        {
            await OpenSheet();
        }
        else if (_currentRow == _maxRowsPerSheet + 1)
        {
            await CloseSheet();
            await OpenSheet();
        }

        var row = new Row { RowIndex = _currentRow };
        for (uint index = 0; index < cells.Length; index++)
        {
            cells[index].CellReference = GetColumnName(index + 1) + _currentRow;
            row.Append(cells[index]);
        }

        _currentRow++;
        await _streamWriter!.WriteLineAsync(row.OuterXml);
    }

    private async Task OpenSheet()
    {
        if (_isSheetOpen)
            return;

        _totalSheets++;
        _currentRow = 1;
        _isSheetOpen = true;

        _fileStream = new FileStream(
            GetTempFileName(_totalSheets),
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

    private static string GetTempFileName(int sheetNumber)
    {
        var fileName = Path.Combine(tempPath, $"sheet{sheetNumber}.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);
        return fileName;
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

    private async Task SaveAsync()
    {
        // await CloseSheet();

        using (var stream = await _storageService.OpenWriteAsync(_fullFileName, _cancellationToken))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false))
        {
            // [Content_Types].xml
            var contentTypesStringBuilder = new StringBuilder();
            for (int i = 1; i <= _totalSheets; i++)
            {
                contentTypesStringBuilder
                    .Append(@$"<Override PartName=""/xl/worksheets/sheet{i}.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>");
            }
            AddXmlFromString(zip, "[Content_Types].xml",
                @$"<?xml version=""1.0"" encoding=""UTF-8""?>
                <Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
                <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
                <Default Extension=""xml""  ContentType=""application/xml""/>
                <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
                {contentTypesStringBuilder}
                </Types>");

            // _rels/.rels
            AddXmlFromString(zip, "_rels/.rels",
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
                <Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
                <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
                </Relationships>");

            // xl/workbook.xml
            var workbook = new StringBuilder();
            for (int i = 1; i <= _totalSheets; i++)
            {
                workbook
                    .Append(@$"<sheet name=""{_sheetBaseName} | {i}"" sheetId=""{i}"" r:id=""rId{i}""/>");
            }
            AddXmlFromString(zip, "xl/workbook.xml",
                @$"<?xml version=""1.0"" encoding=""UTF-8""?>
                <workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
                <sheets>
                    {workbook}
                </sheets>
                </workbook>");

            // xl/_rels/workbook.xml.rels
            var workbookRels = new StringBuilder();
            for (int i = 1; i <= _totalSheets; i++)
            {
                workbookRels
                    .Append(@$"<Relationship Id=""rId{i}"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet{i}.xml""/>");
            }
            AddXmlFromString(zip, "xl/_rels/workbook.xml.rels",
                @$"<?xml version=""1.0"" encoding=""UTF-8""?>
                <Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
                    {workbookRels}
                </Relationships>");

            // xl/worksheets/sheet1.xml => copiar do disco para dentro do ZIP em streaming
            for (int i = 1; i <= _totalSheets; i++)
            {
                var entry = zip.CreateEntry($"xl/worksheets/sheet{i}.xml", CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                using var sourceStream = new FileStream(GetTempFileName(i), FileMode.Open, FileAccess.Read, FileShare.Read);
                await sourceStream.CopyToAsync(entryStream, _cancellationToken);
            }
        }
    }

    private static void AddXmlFromString(ZipArchive zip, string entryName, string xmlContent)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(xmlContent);
    }

    private async Task CloseSheet()
    {
        if (!_isSheetOpen)
            return;

        await _streamWriter!.WriteLineAsync(@"  </sheetData>");
        await _streamWriter!.WriteLineAsync(@"</worksheet>");

        await _streamWriter!.DisposeAsync();
        await _fileStream!.DisposeAsync();

        _isSheetOpen = false;
    }

    // public void Dispose()
    // {
    //     if (_isSheetOpen)
    //     {
    //         _streamWriter!.Dispose();
    //         _fileStream!.Dispose();
    //     }
    // }

    public async ValueTask DisposeAsync()
    {
        await CloseSheet();
        await SaveAsync();
    }
}
