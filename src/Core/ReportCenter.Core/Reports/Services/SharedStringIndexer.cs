using System.Text;
using System.Xml;

namespace ReportCenter.Core.Reports.Services;

public class SharedStringIndexer : IDisposable
{
    private readonly string _tempFilePath;
    private bool _isBuilt = false;

    public SharedStringIndexer()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), "report-center", Guid.NewGuid().ToString(), "sharedString.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(_tempFilePath)!);
    }

    /// <summary>
    /// Lê sharedStrings.xml em streaming e grava em file index.
    /// </summary>
    public async Task BuildIndexAsync(string fullFileName)
    {
        if (_isBuilt)
            return;

        if (!File.Exists(fullFileName))
            return;

        await using (Stream stream = new FileStream(
            fullFileName,
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
        await using (var tempFileStream = new FileStream(
            _tempFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true))
        {
            var writer = new StreamWriter(tempFileStream);

            int index = 0;
            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "si")
                {
                    string strValue = await ReadSharedStringValueAsync(reader);
                    await writer.WriteLineAsync($"{index}|{EscapeForTemp(strValue)}");
                    index++;
                }
            }

            await writer.FlushAsync();
            writer.Close();
        }

        _isBuilt = true;
    }

    /// <summary>
    /// Lê o conteúdo de um <si> em streaming (ct: extension, r:richtext)
    /// </summary>
    private static async Task<string> ReadSharedStringValueAsync(XmlReader reader)
    {
        var value = new StringBuilder();
        using (var subReader = reader.ReadSubtree())
        while (await subReader.ReadAsync())
        {
            if (subReader.NodeType == XmlNodeType.Element && subReader.Name == "t")
                value.Append(await subReader.ReadElementContentAsStringAsync());
        }

        return value.ToString();
    }

    /// <summary>
    /// Busca o valor de sharedStrings pelo índice.
    /// </summary>
    public async Task<string> GetByIndexAsync(int index)
    {
        if (!_isBuilt)
            throw new InvalidOperationException("Index not built");

        using var fs = new FileStream(_tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var sr = new StreamReader(fs);

        string? line;
        while ((line = await sr.ReadLineAsync()) != null)
        {
            int bar = line.IndexOf('|');
            if (bar < 0)
                continue;

            if (int.TryParse(line.AsSpan(0, bar), out int foundIndex) && foundIndex == index)
                return UnescapeFromTemp(line.Substring(bar + 1));
        }

        return "";
    }

    private static string EscapeForTemp(string s) =>
        s.Replace("\n", "\\n").Replace("\r", "\\r").Replace("|", "\\p");

    private static string UnescapeFromTemp(string s) =>
        s.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\p", "|");

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
            Directory.Delete(Path.GetDirectoryName(_tempFilePath)!, true);
    }
}
