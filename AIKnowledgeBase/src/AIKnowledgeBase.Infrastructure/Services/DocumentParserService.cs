using System.Text;
using AIKnowledgeBase.Core.Interfaces;

namespace AIKnowledgeBase.Infrastructure.Services;

public class DocumentParserService : IDocumentParser
{
    private static readonly Dictionary<string, string[]> _supportedTypes = new()
    {
        [".txt"] = new[] { ".txt" },
        [".md"] = new[] { ".md", ".markdown" },
        [".pdf"] = new[] { ".pdf" },
        [".docx"] = new[] { ".docx" },
        [".xlsx"] = new[] { ".xlsx" }
    };

    public bool CanParse(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return _supportedTypes.Values.SelectMany(v => v).Contains(ext);
    }

    public async Task<string> ExtractTextAsync(Stream fileStream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".txt" or ".md" or ".markdown" => await ParseTextAsync(fileStream),
            ".docx" => await ParseDocxAsync(fileStream),
            ".pdf" => await ParsePdfAsync(fileStream),
            ".xlsx" => await ParseExcelAsync(fileStream),
            _ => throw new NotSupportedException($"不支持的文件格式: {ext}")
        };
    }

    private static async Task<string> ParseTextAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static async Task<string> ParseDocxAsync(Stream stream)
    {
        // Uses DocumentFormat.OpenXml (added via NuGet)
        // Simplified implementation - full implementation loads the package and extracts paragraphs
        var sb = new StringBuilder();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;
        // OpenXml SDK code will be added after NuGet restore
        // Placeholder: return raw bytes info for now
        sb.AppendLine("[Word文档内容 - 需要安装DocumentFormat.OpenXml包以完整解析]");
        return sb.ToString();
    }

    private static async Task<string> ParsePdfAsync(Stream stream)
    {
        // Uses PdfPig (added via NuGet)
        var sb = new StringBuilder();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;
        // PdfPig code will be added after NuGet restore
        sb.AppendLine("[PDF文档内容 - 需要安装PdfPig包以完整解析]");
        return sb.ToString();
    }

    private static async Task<string> ParseExcelAsync(Stream stream)
    {
        // Uses ClosedXML or EPPlus (added via NuGet)
        var sb = new StringBuilder();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;
        sb.AppendLine("[Excel文档内容 - 需要安装ClosedXML包以完整解析]");
        return sb.ToString();
    }
}
