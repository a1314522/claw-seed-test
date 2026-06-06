namespace AIKnowledgeBase.Core.DTOs;

public class DocumentUploadRequest
{
    public int CategoryId { get; set; } = 1;
}

public class DocumentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string DocType { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int ChunkCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateDocumentCategoryRequest
{
    public int CategoryId { get; set; }
}

public class DocumentFilterRequest
{
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
