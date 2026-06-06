namespace AIKnowledgeBase.Core.DTOs;

public class SearchRequest
{
    public string Question { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public int TopK { get; set; } = 5;
}

public class SearchResultDto
{
    public string Answer { get; set; } = string.Empty;
    public bool IsMock { get; set; } = true;
    public List<SourceDto> Sources { get; set; } = new();
}

public class SourceDto
{
    public string Source { get; set; } = string.Empty;
    public double Similarity { get; set; }
    public string? Text { get; set; }
    public int? DocumentId { get; set; }
}

public class SearchHistoryDto
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<SourceDto> Sources { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
