using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Attributes;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MediatR;
using RabbitMQ.Client;
using ReportCenter.Common.Providers.Storage.Interfaces;

namespace ReportCenter.App.Domain.Application.Worker;

public class BiggestReportExportTest
{
    private readonly IStorageService _storageService;

    public BiggestReportExportTest(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public void Temp()
    {
        // 1) Definir caminhos absolutos
        string workDir = Path.Combine("xlsx_streaming_demo");
        Directory.CreateDirectory(workDir);

        string sheetXmlPath = Path.Combine(workDir, "sheet1.xml");
        string xlsxPath = Path.Combine(workDir, "relatorio_streaming.xlsx");

        Console.WriteLine($"Dir de trabalho: {workDir}");

        // 2) Gerar o sheet1.xml em streaming (pouca RAM)
        GenerateSheetXml(sheetXmlPath, rows: 1, cols: 20);

        // 3) Empacotar o XML em um .xlsx sem carregar o XML todo na memória
        CreateXlsxFromSheetXml(sheetXmlPath, xlsxPath);

        Console.WriteLine($"OK! Arquivo XLSX em: {xlsxPath}");
        Console.WriteLine($"Tamanho final: {new FileInfo(xlsxPath).Length:N0} bytes");
    }

    public async Task Temp2()
    {
        using (var stream = await _storageService.OpenWriteAsync("Relatório.xlsx"))
        using (var xlWorkbook = new XLWorkbook())
        {
            var _sheet = xlWorkbook.Worksheets.Add("Aba1");

            _sheet.Cell(1, 1).Value = "Column 1 Header";
            _sheet.Cell(1, 2).Value = "Column 2 Header";
            _sheet.Cell(1, 3).Value = "Column 3 Header";
            _sheet.Cell(1, 4).Value = "Column 4 Header";
            _sheet.Cell(1, 5).Value = "Column 5 Header";
            _sheet.Cell(1, 6).Value = "Column 6 Header";
            _sheet.Cell(1, 7).Value = "Column 7 Header";
            _sheet.Cell(1, 8).Value = "Column 8 Header";
            _sheet.Cell(1, 9).Value = "Column 9 Header";
            _sheet.Cell(1, 10).Value = "Column 10 Header";
            _sheet.Cell(1, 11).Value = "Column 11 Header";
            _sheet.Cell(1, 12).Value = "Column 12 Header";
            _sheet.Cell(1, 13).Value = "Column 13 Header";
            _sheet.Cell(1, 14).Value = "Column 14 Header";
            _sheet.Cell(1, 15).Value = "Column 15 Header";
            _sheet.Cell(1, 16).Value = "Column 16 Header";
            _sheet.Cell(1, 17).Value = "Column 17 Header";
            _sheet.Cell(1, 18).Value = "Column 18 Header";
            _sheet.Cell(1, 19).Value = "Column 19 Header";
            _sheet.Cell(1, 20).Value = "Column 20 Header";

            for (int r = 2; r <= 100_000; r++)
            {
                var dado = new TempRow(
                    $@"R{r}C1",
                    $@"R{r}C2",
                    $@"R{r}C3",
                    $@"R{r}C4",
                    $@"R{r}C5",
                    $@"R{r}C6",
                    $@"R{r}C7",
                    $@"R{r}C8",
                    $@"R{r}C9",
                    $@"R{r}C10",
                    $@"R{r}C11",
                    $@"R{r}C12",
                    $@"R{r}C13",
                    $@"R{r}C14",
                    $@"R{r}C15",
                    $@"R{r}C16",
                    $@"R{r}C17",
                    $@"R{r}C18",
                    $@"R{r}C19",
                    $@"R{r}C20"
                );

                _sheet.Row(r).Cell(1).InsertData(new[] { dado });
            }

            xlWorkbook.SaveAs(stream);
        }
    }


    // Gera um sheet1.xml válido para Excel, escrevendo linha a linha (streaming)
    static void GenerateSheetXml(string path, int rows, int cols)
    {
        using var fs = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1 << 15); // 32 KB

        using var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        sw.WriteLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
        sw.WriteLine(@"<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");
        sw.WriteLine(@"  <sheetData>");

        for (uint r = 1; r <= rows; r++)
        {
            // sw.Write($@"    <row r=""{r}"">");
            // for (int c = 1; c <= cols; c++)
            // {
            //     // Inline string: não precisa de sharedStrings.xml
            //     sw.Write($@"<c t=""inlineStr""><is><t>R{r}C{c}</t></is></c>");
            // }
            // sw.WriteLine("</row>");

            var listCells = new Cell[20];
            for (int c = 0; c < cols; c++)
            {
                listCells[c] = new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue($"R{r}C{c}")
                };
            }

            sw.WriteLine(CreateRow(r, listCells));

            if (r % 5000 == 0)
            {
                sw.Flush(); // grava no arquivo periodicamente
                Console.WriteLine($"sheet1.xml: {r} linhas...");
            }
        }

        sw.WriteLine(@"  </sheetData>");
        sw.WriteLine(@"</worksheet>");
    }

    public static string CreateRow(uint rowIndex, Cell[] cells)
    // public static string CreateRow(uint rowIndex, params (string Value, CellValues Type)[] cells)
    {
        var row = new Row { RowIndex = rowIndex };

        uint colIndex = 1;
        foreach (var cell in cells)
        {
            // var cell = new Cell
            // {
            //     CellReference = GetColumnName(colIndex) + rowIndex,
            //     DataType = new EnumValue<CellValues>(cellData.Type),
            //     CellValue = new CellValue(cellData.Value)
            // };

            cell.CellReference = GetColumnName(colIndex) + rowIndex;
            row.Append(cell);
            colIndex++;
        }

        return row.OuterXml;
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

        // string columnName = "";
        // while (columnNumber > 0)
        // {
        //     uint modulo = (columnNumber - 1) % 26;
        //     columnName = Convert.ToChar('A' + modulo) + columnName;
        //     columnNumber = (columnNumber - modulo) / 26;
        // }
        // return columnName;
    }

    // Empacota o sheet1.xml em um .xlsx mínimo, copiando por streaming
    static void CreateXlsxFromSheetXml(string sheetXmlPath, string xlsxPath)
    {
        if (!File.Exists(sheetXmlPath))
            throw new FileNotFoundException("sheet1.xml não encontrado", sheetXmlPath);

        // FileStream com WriteThrough apenas para estudo (não é necessário em produção)
        using var fs = new FileStream(
            xlsxPath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.Read,
            bufferSize: 1 << 14,           // 16 KB
            FileOptions.WriteThrough);

        using var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false);

        // [Content_Types].xml
        AddXmlFromString(zip, "[Content_Types].xml",
            @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
            <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
            <Default Extension=""xml""  ContentType=""application/xml""/>
            <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
            <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
            </Types>");

        // _rels/.rels
        AddXmlFromString(zip, "_rels/.rels",
            @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
            <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
            </Relationships>");

        // xl/workbook.xml
        AddXmlFromString(zip, "xl/workbook.xml",
            @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""
                    xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
            <sheets>
                <sheet name=""Planilha1"" sheetId=""1"" r:id=""rId1""/>
            </sheets>
            </workbook>");

        // xl/_rels/workbook.xml.rels
        AddXmlFromString(zip, "xl/_rels/workbook.xml.rels",
            @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
            <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
            </Relationships>");

        // xl/worksheets/sheet1.xml => copiar do disco para dentro do ZIP em streaming
        var entry = zip.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest);
        using var entryStream = entry.Open();
        using var sourceStream = new FileStream(sheetXmlPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1 << 17); // 128 KB
        sourceStream.CopyTo(entryStream, bufferSize: 1 << 17);
        // Ao sair dos usings, o entry é fechado e os bytes são anexados ao .zip
    }

    static void AddXmlFromString(ZipArchive zip, string entryName, string xmlContent)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(xmlContent);
    }



    // Função auxiliar para escrever texto em um entry do ZIP
    // static void WriteEntry(ZipArchive zip, string entryName, string content)
    // {
    //     var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
    //     using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
    //     writer.Write(content);
    //     // Ao sair do using, esse part é gravado no arquivo físico imediatamente
    // }
}

public record TempRow(
    [property:XLColumn(Header = "coluna1")]
    string coluna1,
    [property:XLColumn(Header = "coluna2")]
    string coluna2,
    [property:XLColumn(Header = "coluna3")]
    string coluna3,
    [property:XLColumn(Header = "coluna4")]
    string coluna4,
    [property:XLColumn(Header = "coluna5")]
    string coluna5,
    [property:XLColumn(Header = "coluna6")]
    string coluna6,
    [property:XLColumn(Header = "coluna7")]
    string coluna7,
    [property:XLColumn(Header = "coluna8")]
    string coluna8,
    [property:XLColumn(Header = "coluna9")]
    string coluna9,
    [property:XLColumn(Header = "coluna10")]
    string coluna10,
    [property:XLColumn(Header = "coluna11")]
    string coluna11,
    [property:XLColumn(Header = "coluna12")]
    string coluna12,
    [property:XLColumn(Header = "coluna13")]
    string coluna13,
    [property:XLColumn(Header = "coluna14")]
    string coluna14,
    [property:XLColumn(Header = "coluna15")]
    string coluna15,
    [property:XLColumn(Header = "coluna16")]
    string coluna16,
    [property:XLColumn(Header = "coluna17")]
    string coluna17,
    [property:XLColumn(Header = "coluna18")]
    string coluna18,
    [property:XLColumn(Header = "coluna19")]
    string coluna19,
    [property:XLColumn(Header = "coluna20")]
    string coluna20
);
